using FlightStatus.Api.Models;

namespace FlightStatus.Api.Providers;

/// <summary>
/// Deterministic stub for QuickFlight — a minimal provider returning status and scheduled times only.
/// No actual times, no terminal, no gate, no delay reason.
/// Note: UA400 is intentionally absent to test single-provider fallback.
/// </summary>
public class QuickFlightStubProvider : IFlightStatusProvider
{
    public string ProviderName => "QuickFlight";

    private record QuickFlightRaw(
        string status,
        DateTime scheduledDeparture,
        DateTime scheduledArrival,
        DateTime updatedAt);

    private static readonly Dictionary<string, QuickFlightRaw> Data =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["AA100"] = new(
                status: "on-time",
                scheduledDeparture: new DateTime(2025, 6, 11, 8, 0, 0, DateTimeKind.Utc),
                scheduledArrival:   new DateTime(2025, 6, 11, 11, 0, 0, DateTimeKind.Utc),
                updatedAt: new DateTime(2025, 6, 11, 7, 30, 0, DateTimeKind.Utc)), // earlier than AeroTrack

            ["BA200"] = new(
                status: "delayed",
                scheduledDeparture: new DateTime(2025, 6, 11, 9, 0, 0, DateTimeKind.Utc),
                scheduledArrival:   new DateTime(2025, 6, 11, 12, 0, 0, DateTimeKind.Utc),
                updatedAt: new DateTime(2025, 6, 11, 8, 25, 0, DateTimeKind.Utc)), // 5 min earlier than AeroTrack

            ["LH300"] = new(
                status: "cancelled",
                scheduledDeparture: new DateTime(2025, 6, 11, 14, 0, 0, DateTimeKind.Utc),
                scheduledArrival:   new DateTime(2025, 6, 11, 17, 0, 0, DateTimeKind.Utc),
                updatedAt: new DateTime(2025, 6, 11, 9, 0, 0, DateTimeKind.Utc)),  // earlier than AeroTrack
        };

    public Task<ProviderResult?> GetStatusAsync(string flightNumber, DateOnly date)
    {
        if (!Data.TryGetValue(flightNumber, out var raw))
            return Task.FromResult<ProviderResult?>(null);

        return Task.FromResult<ProviderResult?>(new ProviderResult(
            ProviderName:       ProviderName,
            RawStatus:          raw.status,
            ScheduledDeparture: raw.scheduledDeparture,
            ScheduledArrival:   raw.scheduledArrival,
            ActualDeparture:    null,
            ActualArrival:      null,
            Terminal:           null,
            Gate:               null,
            DelayReason:        null,
            LastUpdatedUtc:     raw.updatedAt));
    }
}
