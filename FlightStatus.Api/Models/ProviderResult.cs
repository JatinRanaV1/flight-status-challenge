namespace FlightStatus.Api.Models;

/// <summary>
/// Internal model returned by each IFlightStatusProvider after mapping
/// from its own raw response format. Not exposed in the API response.
/// </summary>
public record ProviderResult(
    string ProviderName,
    string RawStatus,
    DateTime ScheduledDeparture,
    DateTime ScheduledArrival,
    DateTime? ActualDeparture,
    DateTime? ActualArrival,
    string? Terminal,
    string? Gate,
    string? DelayReason,
    DateTime LastUpdatedUtc
);
