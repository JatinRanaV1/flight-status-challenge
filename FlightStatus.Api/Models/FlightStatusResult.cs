namespace FlightStatus.Api.Models;

/// <summary>
/// Unified API response model returned by GET /flights/status.
/// AeroTrack-only fields (Terminal, Gate, DelayReason, ActualDeparture, ActualArrival)
/// are null when QuickFlight is the selected provider.
/// </summary>
public record FlightStatusResult(
    string FlightNumber,
    string Date,
    FlightStatusCode Status,
    DateTime ScheduledDeparture,
    DateTime ScheduledArrival,
    DateTime? ActualDeparture,
    DateTime? ActualArrival,
    string? Terminal,
    string? Gate,
    string? DelayReason,
    string? Message,
    DateTime LastUpdatedUtc,
    string ProviderSource
);
