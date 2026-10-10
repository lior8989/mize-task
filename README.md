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

## Assumptions

_To be filled in as the work progresses._

## Claude Code suggestions I changed or rejected

_To be filled in as the work progresses._

## With more time

_To be filled in as the work progresses._
