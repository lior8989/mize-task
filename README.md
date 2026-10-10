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

_To be filled in as the work progresses._

## With more time

_To be filled in as the work progresses._

