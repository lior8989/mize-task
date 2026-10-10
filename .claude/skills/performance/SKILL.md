---
name: performance
description: Performance and concurrency rules for every code change in this repo - async I/O, Dapper/SQLite usage, indexes, transactions, allocations. Use before writing or reviewing any data access or request-path code.
---

# Performance and concurrency

Correctness under concurrency comes first, then speed. Do not trade one for the other silently; call out the tradeoff.

## Request path
- **Async all the way.** Use `async`/`await` with Dapper `*Async` methods and accept `CancellationToken` through controller -> service -> repository. Never block with `.Result` / `.Wait()`.
- **One connection and one short transaction per request.** Open late, close early, keep the transaction to the minimum statements needed.
- **Throttle check + reservation write + stats update happen in one transaction** so concurrent requests cannot exceed the limit or double-insert.
- No per-request allocations that can be avoided on the hot path (no LINQ over large collections, no repeated string building, no needless `ToList()`).

## Data access (Dapper + SQLite)
- **Parameterized SQL only.** Never concatenate input into SQL.
- **Select only needed columns.** No `SELECT *` in repositories.
- **Atomic upserts** (`INSERT ... ON CONFLICT DO UPDATE`) instead of read-then-write where possible. Counters use `Count = Count + 1`.
- **Indexes match the queries.** Primary key on `Reservations(SupplierId, ReservationId)`; index on `RequestLog(SupplierId, RequestedAtMs)`. Add an index when adding a query that needs one, and say which query.
- **Prune bounded tables.** `RequestLog` rows older than the window are deleted so the table stays small.
- SQLite settings: WAL journal mode and a busy timeout so concurrent writers wait instead of failing. Enable `PRAGMA foreign_keys` only if foreign keys are used.
- Avoid N+1 patterns; batch when reading multiple rows.

## Configuration, not constants
- Limit (100) and window (60 s) live in an options class bound from configuration, not magic numbers.

## Process
1. For each data-access or request-path change, note the expected query count per request and the indexes used.
2. Prefer measuring to guessing: if a performance claim matters, add a small test or benchmark rather than asserting it.
3. Do not add caching or micro-optimizations without a stated need; simple and correct beats clever.
