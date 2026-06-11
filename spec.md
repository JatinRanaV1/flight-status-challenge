# spec.md — Data Model & Interface Definitions

> Written and committed before any implementation files.
> This document captures all design decisions, data models, interface contracts, and assumptions
> made during the Analyze and Design phases of the SDLC.

---

## Assumptions

1. **No real flight APIs** — all provider data is stubbed with deterministic hardcoded responses.
2. **"Within 15 minutes"** means `actualDeparture - scheduledDeparture <= 15 minutes` maps to `OnTime`; > 15 maps to `Delayed`. Exactly 15 = OnTime.
3. **Status string from provider takes precedence** over time-derived status. Time-derived status is a fallback only when the provider returns an unrecognised status string but actual times are available.
4. **Tie on `lastUpdatedUtc`** (both providers return same timestamp) — AeroTrack is preferred as it provides richer data.
5. **Provider failure** (exception or null return) is treated as "no result" — not a fatal error. The endpoint returns 200 with `Unknown` status when both fail; individual provider errors are logged.
6. **Date parameter** is the scheduled departure date. Stubs ignore the date and return based on flight number alone (acceptable for stubs; real implementation would filter by date).
7. **CORS** is open in development (`AllowAnyOrigin`) — not for production.
8. **Frontend base URL** points to `http://localhost:5000` (configurable via environment file).
9. **AeroTrack-specific fields** (Terminal, Gate, DelayReason) are `null` when QuickFlight is the selected provider — the UI hides them when null.
10. **No authentication** required for this scope.

---

## Unified Status Enum

```
FlightStatusCode
├── OnTime     — departed/arrived within 15 minutes of schedule
├── Delayed    — departure or arrival pushed beyond 15 minutes
├── Cancelled  — flight will not operate
├── Diverted   — flight landed at a different airport
└── Unknown    — provider returned no usable status
```

---

## Provider Result (internal model — not exposed in API response)

Returned by each `IFlightStatusProvider` implementation after mapping from its own raw format.

| Field               | Type        | AeroTrack | QuickFlight | Notes                         |
|---------------------|-------------|-----------|-------------|-------------------------------|
| ProviderName        | string      | yes       | yes         | "AeroTrack" or "QuickFlight"  |
| RawStatus           | string      | yes       | yes         | Provider-specific status word |
| ScheduledDeparture  | DateTime    | yes       | yes         | UTC                           |
| ScheduledArrival    | DateTime    | yes       | yes         | UTC                           |
| ActualDeparture     | DateTime?   | yes       | null        | null for QuickFlight          |
| ActualArrival       | DateTime?   | yes       | null        | null for QuickFlight          |
| Terminal            | string?     | yes       | null        | null for QuickFlight          |
| Gate                | string?     | yes       | null        | null for QuickFlight          |
| DelayReason         | string?     | yes       | null        | Only when delayed             |
| LastUpdatedUtc      | DateTime    | yes       | yes         | Provider's data freshness     |

---

## API Response Model — FlightStatusResult

```json
{
  "flightNumber": "BA200",
  "date": "2025-06-11",
  "status": "Delayed",
  "scheduledDeparture": "2025-06-11T09:00:00Z",
  "scheduledArrival": "2025-06-11T12:00:00Z",
  "actualDeparture": "2025-06-11T09:45:00Z",
  "actualArrival": "2025-06-11T12:45:00Z",
  "terminal": "T1",
  "gate": "A22",
  "delayReason": "Late inbound aircraft",
  "message": null,
  "lastUpdatedUtc": "2025-06-11T08:30:00Z",
  "providerSource": "AeroTrack"
}
```

When `Unknown` (no providers respond):
```json
{
  "flightNumber": "ZZ999",
  "date": "2025-06-11",
  "status": "Unknown",
  "scheduledDeparture": "0001-01-01T00:00:00",
  "scheduledArrival": "0001-01-01T00:00:00",
  "actualDeparture": null,
  "actualArrival": null,
  "terminal": null,
  "gate": null,
  "delayReason": null,
  "message": "No data available from any provider.",
  "lastUpdatedUtc": "2025-06-11T10:00:00Z",
  "providerSource": "None"
}
```

---

## Provider Interface Contract

```csharp
public interface IFlightStatusProvider
{
    /// <summary>Unique name for this provider, used in logging and ProviderSource field.</summary>
    string ProviderName { get; }

    /// <summary>
    /// Fetches flight status for the given flight number and date.
    /// Returns null if the provider has no data for this flight.
    /// Must NOT throw — exceptions are caught by the endpoint.
    /// </summary>
    Task<ProviderResult?> GetStatusAsync(string flightNumber, DateOnly date);
}
```

---

## Provider Selection Algorithm

```
1. Call both providers concurrently (Task.WhenAll)
2. Catch any exception per provider → treat as null
3. if (aeroTrack == null && quickFlight == null) → return Unknown
4. if (both non-null) → select the one with LATER lastUpdatedUtc
        tie → prefer AeroTrack (richer data)
5. if (only one non-null) → use that one
6. Map selected.RawStatus → FlightStatusCode via lookup table
7. Fallback: if RawStatus unrecognised AND ActualDeparture present
        → derive: delay > 15 min → Delayed, else OnTime
8. Return FlightStatusResult with all fields from selected provider
```

---

## Status Vocabulary Mapping

| AeroTrack raw  | QuickFlight raw | Maps to   |
|----------------|-----------------|-----------|
| ON_TIME        | on-time         | OnTime    |
| DELAYED        | delayed         | Delayed   |
| CANCELLED      | cancelled       | Cancelled |
| DIVERTED       | diverted        | Diverted  |
| (unrecognised) | (unrecognised)  | Unknown (or time-derived) |

---

## Stub Test Data

| Flight | AeroTrack Status | QuickFlight Status | AeroTrack Updated | QF Updated | Expected Winner |
|--------|------------------|--------------------|-------------------|------------|-----------------|
| AA100  | ON_TIME          | on-time            | 07:45 UTC         | 07:30 UTC  | AeroTrack       |
| BA200  | DELAYED (45min)  | delayed            | 08:30 UTC         | 08:25 UTC  | AeroTrack       |
| LH300  | CANCELLED        | cancelled          | 10:00 UTC         | 09:00 UTC  | AeroTrack       |
| UA400  | DIVERTED         | (no data)          | 18:00 UTC         | —          | AeroTrack only  |
| ZZ999  | (no data)        | (no data)          | —                 | —          | Unknown         |

---

## Endpoint

```
GET /flights/status?flightNumber={code}&date={yyyy-MM-dd}

Responses:
  200 OK  → FlightStatusResult
  400 Bad Request → { "error": "..." }  (missing flightNumber or date)
```
