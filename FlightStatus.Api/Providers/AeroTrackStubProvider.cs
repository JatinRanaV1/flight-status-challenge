using FlightStatus.Api.Models;

namespace FlightStatus.Api.Providers;

/// <summary>
/// Deterministic stub for AeroTrack — a verbose provider returning full flight detail.
/// Uses AeroTrack's own field naming conventions internally before mapping to ProviderResult.
/// </summary>
public class AeroTrackStubProvider : IFlightStatusProvider
{
    public string ProviderName => "AeroTrack";

    // Simulates AeroTrack's raw verbose response shape
    private record AeroTrackRaw(
        string flt_status,
        DateTime sched_dep,
        DateTime sched_arr,
        DateTime? act_dep,
        DateTime? act_arr,
        string? terminal_id,
        string? gate_code,
        string? delay_rsn,
        DateTime updated_ts);

    private static readonly Dictionary<string, AeroTrackRaw> Data =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["AA100"] = new(
                flt_status: "ON_TIME",
                sched_dep: new DateTime(2025, 6, 11, 8, 0, 0, DateTimeKind.Utc),
                sched_arr: new DateTime(2025, 6, 11, 11, 0, 0, DateTimeKind.Utc),
                act_dep:   new DateTime(2025, 6, 11, 7, 55, 0, DateTimeKind.Utc),
                act_arr:   new DateTime(2025, 6, 11, 10, 55, 0, DateTimeKind.Utc),
                terminal_id: "T2",
                gate_code: "B14",
                delay_rsn: null,
                updated_ts: new DateTime(2025, 6, 11, 7, 45, 0, DateTimeKind.Utc)), // later than QF

            ["BA200"] = new(
                flt_status: "DELAYED",
                sched_dep: new DateTime(2025, 6, 11, 9, 0, 0, DateTimeKind.Utc),
                sched_arr: new DateTime(2025, 6, 11, 12, 0, 0, DateTimeKind.Utc),
                act_dep:   new DateTime(2025, 6, 11, 9, 45, 0, DateTimeKind.Utc),   // 45 min late
                act_arr:   new DateTime(2025, 6, 11, 12, 45, 0, DateTimeKind.Utc),
                terminal_id: "T1",
                gate_code: "A22",
                delay_rsn: "Late inbound aircraft",
                updated_ts: new DateTime(2025, 6, 11, 8, 30, 0, DateTimeKind.Utc)), // 5 min later than QF

            ["LH300"] = new(
                flt_status: "CANCELLED",
                sched_dep: new DateTime(2025, 6, 11, 14, 0, 0, DateTimeKind.Utc),
                sched_arr: new DateTime(2025, 6, 11, 17, 0, 0, DateTimeKind.Utc),
                act_dep:   null,
                act_arr:   null,
                terminal_id: null,
                gate_code: null,
                delay_rsn: "Crew unavailability",
                updated_ts: new DateTime(2025, 6, 11, 10, 0, 0, DateTimeKind.Utc)),

            ["UA400"] = new(
                flt_status: "DIVERTED",
                sched_dep: new DateTime(2025, 6, 11, 16, 0, 0, DateTimeKind.Utc),
                sched_arr: new DateTime(2025, 6, 11, 19, 0, 0, DateTimeKind.Utc),
                act_dep:   new DateTime(2025, 6, 11, 16, 10, 0, DateTimeKind.Utc),
                act_arr:   new DateTime(2025, 6, 11, 19, 30, 0, DateTimeKind.Utc),
                terminal_id: "T3",
                gate_code: "C05",
                delay_rsn: "Weather at destination",
                updated_ts: new DateTime(2025, 6, 11, 18, 0, 0, DateTimeKind.Utc)),
        };

    public Task<ProviderResult?> GetStatusAsync(string flightNumber, DateOnly date)
    {
        if (!Data.TryGetValue(flightNumber, out var raw))
            return Task.FromResult<ProviderResult?>(null);

        return Task.FromResult<ProviderResult?>(new ProviderResult(
            ProviderName:       ProviderName,
            RawStatus:          raw.flt_status,
            ScheduledDeparture: raw.sched_dep,
            ScheduledArrival:   raw.sched_arr,
            ActualDeparture:    raw.act_dep,
            ActualArrival:      raw.act_arr,
            Terminal:           raw.terminal_id,
            Gate:               raw.gate_code,
            DelayReason:        raw.delay_rsn,
            LastUpdatedUtc:     raw.updated_ts));
    }
}
