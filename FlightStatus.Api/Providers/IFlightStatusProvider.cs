using FlightStatus.Api.Models;

namespace FlightStatus.Api.Providers;

/// <summary>
/// Abstraction over an external flight data provider.
/// Implementations must be safe to call concurrently and must not throw —
/// exceptions are caught by the endpoint and treated as no-data.
/// </summary>
public interface IFlightStatusProvider
{
    /// <summary>Unique provider name used in logging and the ProviderSource response field.</summary>
    string ProviderName { get; }

    /// <summary>
    /// Returns the provider's flight status data for the given flight and date,
    /// or null if the provider has no record for this flight.
    /// </summary>
    Task<ProviderResult?> GetStatusAsync(string flightNumber, DateOnly date);
}
