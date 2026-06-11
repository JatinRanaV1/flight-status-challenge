# prompts.md — AI Prompt Log

All significant GitHub Copilot prompts used during this challenge, with notes on acceptance, modification, or rejection.

---

## 1. Interface Design

**Prompt:**
> "I'm building a flight status aggregator in .NET 8 Minimal API. I need an interface called
> `IFlightStatusProvider` that any provider stub can implement. It should return a nullable
> `ProviderResult` so missing data is explicit. Include a `ProviderName` string property
> for logging and tracing. The result should be a C# record with scheduled/actual departure
> and arrival as nullable DateTime, plus terminal, gate, and delayReason as nullable strings,
> and a lastUpdatedUtc DateTime for freshness comparison."

**Accepted:** Yes — generated interface and record skeleton correctly.
**Modified:** Added `RawStatus` string field to `ProviderResult` so each provider stores its
own status vocabulary and the normaliser handles the mapping centrally rather than in each stub.

---

## 2. Status Normalisation Logic

**Prompt:**
> "Write a C# class `FlightStatusNormaliser` with a `Normalise` method. It takes two nullable
> `ProviderResult` objects (aeroTrack, quickFlight), selects the one with the later
> `lastUpdatedUtc` (tie goes to AeroTrack as richer source), then maps the selected provider's
> `RawStatus` string to a `FlightStatusCode` enum (OnTime, Delayed, Cancelled, Diverted, Unknown).
> If both are null, return Unknown with message 'No data available from any provider.'
> If the status string is unrecognised but ActualDeparture is present, derive:
> delay > 15 minutes = Delayed, else OnTime. Return a `FlightStatusResult` record."

**Accepted:** Core logic accepted.
**Rejected:** Copilot wrapped the entire method in a try/catch swallowing all exceptions.
Removed it — exceptions in the normaliser indicate bugs, not expected failures. Provider
exceptions are already handled upstream in the endpoint.
**Modified:** Extracted the status vocabulary dictionary as a static field rather than
rebuilding it on every call.

---

## 3. AeroTrack Stub Provider

**Prompt:**
> "Create a deterministic stub called `AeroTrackStubProvider` implementing `IFlightStatusProvider`.
> Hardcode 4 flights using AeroTrack's verbose internal field naming (flt_status, sched_dep,
> sched_arr, act_dep, act_arr, terminal_id, gate_code, delay_rsn, updated_ts):
> AA100 = ON_TIME (5 min early actual dep, terminal T2, gate B14, lastUpdated 07:45 UTC),
> BA200 = DELAYED (45 min late actual dep, terminal T1, gate A22, reason 'Late inbound aircraft',
> lastUpdated 08:30 UTC — 5 minutes LATER than QuickFlight to test winner selection),
> LH300 = CANCELLED (no actual times, reason 'Crew unavailability', lastUpdated 10:00 UTC),
> UA400 = DIVERTED (terminal T3, gate C05, reason 'Weather at destination', lastUpdated 18:00 UTC).
> UA400 should NOT exist in QuickFlight to test single-provider fallback."

**Accepted:** Yes — used as-is.
**Note:** Copilot used object initialisers instead of primary constructor syntax for the inner
record. Kept it as-is — cleaner with this many fields.

---

## 4. QuickFlight Stub Provider

**Prompt:**
> "Create `QuickFlightStubProvider` implementing `IFlightStatusProvider`. QuickFlight returns
> minimal data: status string (on-time, delayed, cancelled, diverted), scheduledDeparture,
> scheduledArrival, updatedAt — NO actual times, NO terminal/gate/delay reason.
> Hardcode AA100 (on-time, lastUpdated 07:30 UTC — earlier than AeroTrack),
> BA200 (delayed, lastUpdated 08:25 UTC — 5 min earlier than AeroTrack),
> LH300 (cancelled, lastUpdated 09:00 UTC — earlier than AeroTrack).
> Do NOT include UA400 — QuickFlight has no data for it."

**Accepted:** Yes — correct.
**Modified:** Made the lookup dictionary case-insensitive (`StringComparer.OrdinalIgnoreCase`)
so it handles any future casing variations in flight number inputs.

---

## 5. Minimal API Endpoint

**Prompt:**
> "Write a .NET 8 Minimal API endpoint: GET /flights/status?flightNumber=&date=
> It receives IEnumerable<IFlightStatusProvider> and FlightStatusNormaliser via DI.
> Validate that flightNumber is not empty → 400 if missing.
> Validate date parses as DateOnly in yyyy-MM-dd format → 400 if missing or invalid.
> Call both providers concurrently with Task.WhenAll, wrapping each in a try/catch
> that logs the error and returns null for that provider.
> Pass both results to the normaliser and return 200 with the FlightStatusResult.
> Log the incoming request and the final status + provider source."

**Accepted:** Structure accepted.
**Rejected:** Copilot used `Results.Problem()` for provider failures — removed it.
Provider failures are handled silently (logged + null), not surfaced as HTTP errors.
**Modified:** Extracted `FetchSafe` as a local static function for readability.

---

## 6. DI Registration in Program.cs

**Prompt:**
> "Register the two provider stubs as IFlightStatusProvider using AddSingleton so they
> are collected as IEnumerable<IFlightStatusProvider>. Register FlightStatusNormaliser
> as singleton. Add CORS allowing any origin for development. Add Swagger/OpenAPI."

**Accepted:** Yes — straightforward DI wiring.

---

## 7. xUnit Tests

**Prompt:**
> "Generate xUnit tests for FlightStatusNormaliser. Cover these cases:
> 1. Both providers null → Unknown, ProviderSource='None', Message non-null
> 2. Only AeroTrack → uses AeroTrack
> 3. Only QuickFlight → uses QuickFlight
> 4. Both respond, AeroTrack has later timestamp → AeroTrack wins
> 5. Both respond, QuickFlight has later timestamp → QuickFlight wins
> 6. Theory: all 8 raw status strings map to correct FlightStatusCode
> 7. ActualDep exactly 15 min late → OnTime (boundary)
> 8. ActualDep 16 min late → Delayed
> 9. AeroTrack fields (terminal, gate, delayReason) propagate to result
> 10. Tie on timestamp → AeroTrack preferred
> Use a private helper MakeResult to reduce boilerplate."

**Accepted:** All 10 test cases accepted.
**Added manually:** Verified that when QuickFlight is the selected provider, Terminal/Gate/
DelayReason are null in the result (important for the UI conditional rendering guarantee).

---

## 8. Angular Model + Service

**Prompt:**
> "Generate a TypeScript interface `FlightStatusResult` matching the .NET API response.
> All AeroTrack-only fields (terminal, gate, delayReason, actualDeparture, actualArrival)
> should be typed as `string | null`. Status should be a union type of the 5 possible values.
> Then generate an Angular injectable service `FlightStatusService` that calls
> GET http://localhost:5000/flights/status with HttpParams for flightNumber and date.
> Returns Observable<FlightStatusResult>."

**Accepted:** Yes.
**Modified:** Added a typed `ApiErrorResponse` interface for 400 error handling in the component.

---

## 9. Search Form Component

**Prompt:**
> "Create an Angular 17 standalone SearchFormComponent. Has two inputs: text field for
> flight number, date input for date. On submit, calls FlightStatusService and emits
> the result via @Output() EventEmitter. Show a loading state on the button while the
> request is in flight. Handle HttpErrorResponse: emit the error message string.
> Use FormsModule for template-driven form with required validation."

**Accepted:** Component structure accepted.
**Modified:** Changed @Output() to emit a typed object `{ result, error, loading }` so
the parent can track all three states in one event rather than three separate emitters.

---

## 10. Result Card Component

**Prompt:**
> "Create an Angular 17 standalone ResultCardComponent. Takes FlightStatusResult as @Input().
> Takes errorMessage string as @Input(). Applies CSS class based on status:
> on-time → green, delayed → amber, cancelled/diverted → red, unknown → grey.
> Conditionally render terminal, gate, delayReason only when non-null.
> Show error card when errorMessage is set."

**Accepted:** Yes.
**Modified:** Used `@switch` block (Angular 17 control flow) instead of multiple *ngIf
for the status colour class — cleaner and evaluator-visible modern Angular usage.
