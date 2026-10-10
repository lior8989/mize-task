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

## Claude Code suggestions I changed or rejected

_To be filled in as the work progresses._

## With more time

_To be filled in as the work progresses._

