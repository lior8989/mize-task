# mize-task

Supplier Feed Throttle & Dedup Service: an ASP.NET Core Web API (.NET 8) backed by SQLite and Dapper.

## Running

```
dotnet run --project src/SupplierFeed.Api
```

Swagger UI is served at `/swagger` (the launch profile opens it automatically).

## Tests

```
dotnet test SupplierFeed.slnx
```

## Layout

- `src/SupplierFeed.Api` - the Web API (controllers, services, repositories).
- `tests/SupplierFeed.Tests` - unit and integration tests.
- `.claude/skills` - project skills (SOLID/patterns, performance, docs + tests) used during development.

## Design

Request flow: controller -> service -> repositories (Dapper) -> SQLite. Repositories are created per request by a unit of work, so the throttle check, reservation write and stats update share one transaction.

## Assumptions

- **SQLite** is the database: no install needed. Multiple server instances are only correct if they share the same database; in production this would move to SQL Server/Postgres (the SQL is kept simple for that).
- **Reservation identity** is (`supplierId`, `reservationId`): two suppliers may reuse the same reservation id.
- **Price** is stored as TEXT (exact decimal), because SQLite has no decimal type and floats are unsafe for money. Timestamps are ISO 8601 UTC TEXT.
- **Throttling state lives in the database** (`RequestLog`) rather than memory, so the limit holds across instances. Write requests take the SQLite write lock up front (`BEGIN IMMEDIATE`) so the check-then-write cannot interleave; reads do not.
- **Throttle semantics:** at most 100 *accepted* requests per supplier in any rolling 60s window (configurable under `Throttle`). The 101st gets `429` with a `Retry-After` header. Rejected requests are not logged, so hammering while throttled does not extend the penalty. Duplicates count as requests; invalid (400) requests do not reach the limiter.
- **Dedup:** "same details" means same room, check-in, check-out and price. A resend that only bumps `updatedAtUtc` is treated as unchanged.
- **Out-of-order retries:** if details differ but the incoming `updatedAtUtc` is older than the stored one, the stored data is kept (outcome `Stale`, counted as unchanged) rather than being overwritten by an old retry.
- **Responses:** `201 Created` for new, `200` with `changed: true/false` for updates / no-ops, `400` for invalid input (missing ids, `checkOut <= checkIn`, negative price), `429` when throttled.
- **Stats:** `GET /api/reservations/stats/{supplierId}` returns `ingested` (created + updated), `unchanged` and `throttled`; an unknown supplier returns zeros rather than 404.

## Claude Code suggestions I changed or rejected

- **Price storage:** Claude offered several options for the SQLite price column; I chose exact decimal as TEXT over REAL/integer-cents, to avoid float rounding without changing the API contract.
- **Table design:** I asked Claude to explain `RequestLog` and `SupplierStats` before accepting them, then kept both: the log is the source of truth for the rolling window, the counters make the stats endpoint a single-row read.
- **Service design:** I steered toward repositories behind interfaces with a unit of work (rather than a DbContext-style abstraction), injected into services/controllers, and asked for project skills (SOLID/patterns, performance, docs + tests) that Claude applies on every change.
- **Workflow:** every change goes on its own branch and PR that I review before it is merged to `main`.
- **Claude's .NET 10 templates:** the SDK only offered `net10.0`; the projects were retargeted to `net8.0` by hand to match the brief.

## With more time

- **Real database.** SQLite serialises all writers, which caps throughput. SQL Server/Postgres with a per-supplier atomic counter (or Redis sliding-window) would scale better; multiple instances today must share one database file.
- **Throttle cost.** Each accepted request inserts a log row and prunes old ones; a fixed-bucket counter would be cheaper at the price of a less exact window.
- **Idempotency edge cases:** configurable stale-update policy, and an optimistic-concurrency token on reservations.
- **Operational hardening:** auth per supplier, structured logging/metrics, a background job to purge idle suppliers' log rows, rate-limit headers on successful responses, and load tests.

## Claude Code transcript

The full, unedited Claude Code conversation is in [docs/claude-code-transcript.txt](docs/claude-code-transcript.txt). /export was not available in this environment, so it is a text export of the saved session files (the original session, the short esume sessions that hit the API usage limit, and the session that finished the work), in chronological order. It omits internal reasoning blocks and anything after the moment it was generated.
