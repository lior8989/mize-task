---
name: design-patterns-solid
description: Apply SOLID principles and fitting design patterns to every code change and design decision in this repo (controllers, services, repositories, DI wiring, tests). Use before writing or reviewing any C# code.
---

# Design patterns and SOLID

Apply to the whole request flow: controller -> service -> repository -> SQLite, and to the tests covering it.

## Architecture rules for this repo
- **Controllers are thin.** Bind and validate the request, call one service, map the result to an HTTP response. No business rules, no SQL.
- **Services own business rules** (dedup, stale-update handling, throttling). They depend on interfaces only.
- **Repositories own SQL** for exactly one table each. No business decisions inside them.
- **No DbContext.** Dapper is used through `IDbConnectionFactory` plus a unit of work / transaction passed to repositories.
- **DTOs are separate from entities.** Request/response models never double as DB rows.
- **Time comes from `IClock`**, never `DateTime.UtcNow` directly, so logic is testable.
- **Constructor injection only.** No service locator, no static state, no `new` of collaborators inside services.

## SOLID checklist (verify on every change)
- **S** - Does each class have one reason to change? Split if a class mixes HTTP, rules, and SQL.
- **O** - Can new behavior (e.g. a new throttle policy) be added by adding code rather than editing working code? Prefer options classes and strategy-style interfaces over `if` chains.
- **L** - Do implementations honor the full contract of their interface (including exceptions and null behavior)? Test doubles must too.
- **I** - Are interfaces small and role-specific? Don't hand a consumer methods it never calls.
- **D** - Do high-level classes depend on abstractions? Register everything in DI; keep composition in `Program.cs` / extension methods.

## Patterns: use when they fit, don't force them
- Repository, Unit of Work, Options pattern (configuration such as limit=100 / window=60s), Strategy (swappable policies), Factory (`IDbConnectionFactory`).
- Prefer the simplest design that satisfies the checklist. Do not add a pattern with a single implementation and no seam that needs it. State the reason in one line when adding one.

## Process
1. Before coding, state which classes/interfaces change and why, in one or two lines.
2. After coding, re-run the SOLID checklist mentally and fix violations before committing.
3. Every new class with logic gets unit tests that cover public behavior, not implementation details (see the documentation-swagger skill for test rules).
