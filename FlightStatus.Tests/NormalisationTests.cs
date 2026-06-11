using FlightStatus.Api.Models;
using FlightStatus.Api.Services;
using Xunit;

namespace FlightStatus.Tests;

public class NormalisationTests
{
    private readonly FlightStatusNormaliser _normaliser = new();
    private static readonly DateOnly TestDate = new(2025, 6, 11);

    // Helper: build a ProviderResult with sensible defaults
    private static ProviderResult Make(
        string providerName,
        string rawStatus,
        DateTime? actualDep    = null,
        DateTime? lastUpdated  = null,
        string? terminal       = null,
        string? gate           = null,
        string? delayReason    = null)
    {
        var sched = new DateTime(2025, 6, 11, 8, 0, 0, DateTimeKind.Utc);
        return new ProviderResult(
            ProviderName:       providerName,
            RawStatus:          rawStatus,
            ScheduledDeparture: sched,
            ScheduledArrival:   sched.AddHours(3),
            ActualDeparture:    actualDep,
            ActualArrival:      null,
            Terminal:           terminal,
            Gate:               gate,
            DelayReason:        delayReason,
            LastUpdatedUtc:     lastUpdated ?? DateTime.UtcNow);
    }

    // ── Provider selection ──────────────────────────────────────────────────

    [Fact]
    public void BothNull_ReturnsUnknown_WithMessage()
    {
        var result = _normaliser.Normalise("XX999", TestDate, null, null);

        Assert.Equal(FlightStatusCode.Unknown, result.Status);
        Assert.Equal("None", result.ProviderSource);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public void OnlyAeroTrack_UsesAeroTrack()
    {
        var at = Make("AeroTrack", "ON_TIME");

        var result = _normaliser.Normalise("AA100", TestDate, at, null);

        Assert.Equal(FlightStatusCode.OnTime, result.Status);
        Assert.Equal("AeroTrack", result.ProviderSource);
    }

    [Fact]
    public void OnlyQuickFlight_UsesQuickFlight()
    {
        var qf = Make("QuickFlight", "on-time");

        var result = _normaliser.Normalise("AA100", TestDate, null, qf);

        Assert.Equal(FlightStatusCode.OnTime, result.Status);
        Assert.Equal("QuickFlight", result.ProviderSource);
    }

    [Fact]
    public void BothRespond_AeroTrackHasLaterTimestamp_SelectsAeroTrack()
    {
        var now = new DateTime(2025, 6, 11, 10, 0, 0, DateTimeKind.Utc);
        var at  = Make("AeroTrack",   "DELAYED",  lastUpdated: now.AddMinutes(5));
        var qf  = Make("QuickFlight", "on-time",  lastUpdated: now);

        var result = _normaliser.Normalise("BA200", TestDate, at, qf);

        Assert.Equal("AeroTrack", result.ProviderSource);
        Assert.Equal(FlightStatusCode.Delayed, result.Status);
    }

    [Fact]
    public void BothRespond_QuickFlightHasLaterTimestamp_SelectsQuickFlight()
    {
        var now = new DateTime(2025, 6, 11, 10, 0, 0, DateTimeKind.Utc);
        var at  = Make("AeroTrack",   "DELAYED",  lastUpdated: now);
        var qf  = Make("QuickFlight", "on-time",  lastUpdated: now.AddMinutes(5));

        var result = _normaliser.Normalise("BA200", TestDate, at, qf);

        Assert.Equal("QuickFlight", result.ProviderSource);
        Assert.Equal(FlightStatusCode.OnTime, result.Status);
    }

    [Fact]
    public void TieOnTimestamp_PrefersAeroTrack()
    {
        var ts = new DateTime(2025, 6, 11, 10, 0, 0, DateTimeKind.Utc);
        var at = Make("AeroTrack",   "ON_TIME",  lastUpdated: ts);
        var qf = Make("QuickFlight", "on-time",  lastUpdated: ts);

        var result = _normaliser.Normalise("AA100", TestDate, at, qf);

        Assert.Equal("AeroTrack", result.ProviderSource);
    }

    // ── Status mapping — both vocabularies ─────────────────────────────────

    [Theory]
    [InlineData("ON_TIME",   FlightStatusCode.OnTime)]
    [InlineData("DELAYED",   FlightStatusCode.Delayed)]
    [InlineData("CANCELLED", FlightStatusCode.Cancelled)]
    [InlineData("DIVERTED",  FlightStatusCode.Diverted)]
    [InlineData("on-time",   FlightStatusCode.OnTime)]
    [InlineData("delayed",   FlightStatusCode.Delayed)]
    [InlineData("cancelled", FlightStatusCode.Cancelled)]
    [InlineData("diverted",  FlightStatusCode.Diverted)]
    public void StatusMapping_AllVocabularies(string rawStatus, FlightStatusCode expected)
    {
        var at = Make("AeroTrack", rawStatus);

        var result = _normaliser.Normalise("XX100", TestDate, at, null);

        Assert.Equal(expected, result.Status);
    }

    // ── 15-minute boundary ─────────────────────────────────────────────────

    [Fact]
    public void Exactly15MinLate_IsOnTime()
    {
        var sched  = new DateTime(2025, 6, 11, 8, 0, 0, DateTimeKind.Utc);
        var actual = sched.AddMinutes(15);
        var pr = new ProviderResult("AeroTrack", "UNKNOWN_STATUS",
            sched, sched.AddHours(3), actual, null, null, null, null, DateTime.UtcNow);

        var result = _normaliser.Normalise("XX001", TestDate, pr, null);

        Assert.Equal(FlightStatusCode.OnTime, result.Status);
    }

    [Fact]
    public void SixteenMinLate_IsDelayed()
    {
        var sched  = new DateTime(2025, 6, 11, 8, 0, 0, DateTimeKind.Utc);
        var actual = sched.AddMinutes(16);
        var pr = new ProviderResult("AeroTrack", "UNKNOWN_STATUS",
            sched, sched.AddHours(3), actual, null, null, null, null, DateTime.UtcNow);

        var result = _normaliser.Normalise("XX002", TestDate, pr, null);

        Assert.Equal(FlightStatusCode.Delayed, result.Status);
    }

    // ── Field propagation ──────────────────────────────────────────────────

    [Fact]
    public void AeroTrackFields_PropagateToResult()
    {
        var at = Make("AeroTrack", "DELAYED", terminal: "T1", gate: "A22", delayReason: "Weather");

        var result = _normaliser.Normalise("BA200", TestDate, at, null);

        Assert.Equal("T1",      result.Terminal);
        Assert.Equal("A22",     result.Gate);
        Assert.Equal("Weather", result.DelayReason);
    }

    [Fact]
    public void QuickFlightSelected_AeroTrackOnlyFieldsAreNull()
    {
        var now = DateTime.UtcNow;
        var at  = Make("AeroTrack",   "DELAYED",  lastUpdated: now,               terminal: "T1", gate: "A22");
        var qf  = Make("QuickFlight", "on-time",  lastUpdated: now.AddMinutes(5));

        var result = _normaliser.Normalise("BA200", TestDate, at, qf);

        Assert.Equal("QuickFlight", result.ProviderSource);
        Assert.Null(result.Terminal);
        Assert.Null(result.Gate);
        Assert.Null(result.DelayReason);
    }

    [Fact]
    public void UnknownRawStatus_NoActualTime_ReturnsUnknown()
    {
        var pr = Make("AeroTrack", "SOME_FUTURE_STATUS");

        var result = _normaliser.Normalise("XX003", TestDate, pr, null);

        Assert.Equal(FlightStatusCode.Unknown, result.Status);
    }
}
