using FlightStatus.Api.Models;

namespace FlightStatus.Api.Services;

/// <summary>
/// Selects the best provider result and normalises it into a unified FlightStatusResult.
/// Selection: both present → later lastUpdatedUtc wins (tie → AeroTrack).
/// Status mapping: lookup table → fallback time-derivation → Unknown.
/// </summary>
public class FlightStatusNormaliser
{
    private static readonly Dictionary<string, FlightStatusCode> StatusMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // AeroTrack vocabulary
            ["ON_TIME"]   = FlightStatusCode.OnTime,
            ["DELAYED"]   = FlightStatusCode.Delayed,
            ["CANCELLED"] = FlightStatusCode.Cancelled,
            ["DIVERTED"]  = FlightStatusCode.Diverted,
            // QuickFlight vocabulary
            ["on-time"]   = FlightStatusCode.OnTime,
            ["delayed"]   = FlightStatusCode.Delayed,
            ["cancelled"] = FlightStatusCode.Cancelled,
            ["diverted"]  = FlightStatusCode.Diverted,
        };

    public FlightStatusResult Normalise(
        string flightNumber,
        DateOnly date,
        ProviderResult? aeroTrack,
        ProviderResult? quickFlight)
    {
        if (aeroTrack is null && quickFlight is null)
        {
            return new FlightStatusResult(
                FlightNumber:       flightNumber,
                Date:               date.ToString("yyyy-MM-dd"),
                Status:             FlightStatusCode.Unknown,
                ScheduledDeparture: default,
                ScheduledArrival:   default,
                ActualDeparture:    null,
                ActualArrival:      null,
                Terminal:           null,
                Gate:               null,
                DelayReason:        null,
                Message:            "No data available from any provider.",
                LastUpdatedUtc:     DateTime.UtcNow,
                ProviderSource:     "None");
        }

        var selected = SelectProvider(aeroTrack, quickFlight);
        var status   = MapStatus(selected);

        return new FlightStatusResult(
            FlightNumber:       flightNumber,
            Date:               date.ToString("yyyy-MM-dd"),
            Status:             status,
            ScheduledDeparture: selected.ScheduledDeparture,
            ScheduledArrival:   selected.ScheduledArrival,
            ActualDeparture:    selected.ActualDeparture,
            ActualArrival:      selected.ActualArrival,
            Terminal:           selected.Terminal,
            Gate:               selected.Gate,
            DelayReason:        selected.DelayReason,
            Message:            null,
            LastUpdatedUtc:     selected.LastUpdatedUtc,
            ProviderSource:     selected.ProviderName);
    }

    private static ProviderResult SelectProvider(ProviderResult? aeroTrack, ProviderResult? quickFlight)
    {
        if (aeroTrack is null) return quickFlight!;
        if (quickFlight is null) return aeroTrack;

        // Prefer the more recently updated result; tie goes to AeroTrack (richer data)
        return quickFlight.LastUpdatedUtc > aeroTrack.LastUpdatedUtc ? quickFlight : aeroTrack;
    }

    private static FlightStatusCode MapStatus(ProviderResult result)
    {
        if (StatusMap.TryGetValue(result.RawStatus, out var mapped))
            return mapped;

        // Fallback: derive from actual vs scheduled departure time
        if (result.ActualDeparture.HasValue)
        {
            var delayMinutes = (result.ActualDeparture.Value - result.ScheduledDeparture).TotalMinutes;
            return delayMinutes > 15 ? FlightStatusCode.Delayed : FlightStatusCode.OnTime;
        }

        return FlightStatusCode.Unknown;
    }
}
