# Flight Status — SkyRoute Platform

Full-stack flight status lookup feature built as part of the SkyRoute platform challenge.

## Architecture

```
flight-status-ui/     Angular 17 standalone SPA
FlightStatus.Api/     .NET 8 Minimal API
FlightStatus.Tests/   xUnit unit tests
```

### Key Design Decisions

- **Provider abstraction**: `IFlightStatusProvider` interface with two deterministic stub implementations (`AeroTrackStubProvider`, `QuickFlightStubProvider`). The endpoint receives `IEnumerable<IFlightStatusProvider>` via DI — no concrete types referenced.
- **Normalisation**: `FlightStatusNormaliser` maps provider-specific status vocabularies to a single `FlightStatusCode` enum. When both providers respond, the one with the later `lastUpdatedUtc` wins. Tie breaks to AeroTrack (richer data source).
- **Graceful degradation**: Provider exceptions are caught per-provider; the endpoint never returns 500 due to a provider failure. Falls back to `Unknown` with a message when both fail.
- **AeroTrack-only fields**: `Terminal`, `Gate`, `DelayReason` are `null` when QuickFlight is selected — the UI conditionally hides them.

---

## Prerequisites

| Tool    | Version  |
|---------|----------|
| .NET    | 8.0+     |
| Node.js | 18+      |
| Angular CLI | 17+ |

Install Angular CLI if not present:
```bash
npm install -g @angular/cli
```

---

## Running the Backend

```bash
cd FlightStatus.Api
dotnet restore
dotnet run
```

API listens on `http://localhost:5000`.

Test it:
```bash
curl "http://localhost:5000/flights/status?flightNumber=AA100&date=2025-06-11"
curl "http://localhost:5000/flights/status?flightNumber=BA200&date=2025-06-11"
curl "http://localhost:5000/flights/status?flightNumber=LH300&date=2025-06-11"
curl "http://localhost:5000/flights/status?flightNumber=UA400&date=2025-06-11"
curl "http://localhost:5000/flights/status?flightNumber=ZZ999&date=2025-06-11"
```

Swagger UI available at `http://localhost:5000/swagger` in development.

---

## Running the Tests

```bash
cd FlightStatus.Tests
dotnet test
```

Expected: all tests pass. Coverage includes normalisation rules, provider selection logic, edge cases (15-min boundary, tie-break, single-provider fallback).

---

## Running the Frontend

```bash
cd flight-status-ui
npm install
ng serve
```

UI available at `http://localhost:4200`.

Ensure the backend is running first. The service URL is configured in `src/environments/environment.ts`.

---

## Stub Flight Data

| Flight | Status    | Notes                              |
|--------|-----------|------------------------------------|
| AA100  | OnTime    | Both providers respond; AeroTrack wins (later timestamp) |
| BA200  | Delayed   | 45 min late; AeroTrack wins (later timestamp) |
| LH300  | Cancelled | Both providers respond             |
| UA400  | Diverted  | AeroTrack only (QuickFlight has no data) |
| ZZ999  | Unknown   | Neither provider has data          |

---

## Copilot Usage

See `prompts.md` for a full log of all AI prompts used, with notes on what was accepted, modified, or rejected.

---

## Assumptions

See `spec.md` for the full list. Key ones:
- Stub providers ignore the `date` parameter — responses are keyed by flight number only
- Exactly 15 minutes late = OnTime (boundary inclusive)
- CORS is open in development only
