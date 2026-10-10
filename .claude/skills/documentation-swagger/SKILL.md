---
name: documentation-swagger
description: Documentation and testing rules for this repo - XML docs on every public member surfaced in Swagger, plus unit tests for all logic. Use whenever adding or changing public types, endpoints, or logic.
---

# Documentation (visible in Swagger) and tests

## XML documentation
- **Every public type and member gets `///` XML docs**: `<summary>`, plus `<param>`, `<returns>`, and `<exception>` where relevant. This includes DTOs and their properties, enums and their values, interfaces, services, repositories, and controllers.
- **Endpoints** additionally document: purpose, each possible response with `[ProducesResponseType]` (200, 201, 400, 429, ...), and an example request/response where helpful (`<example>` / `<remarks>`).
- **Swagger must show it.** The API project generates the XML documentation file (`GenerateDocumentationFile`), and Swashbuckle is configured with `IncludeXmlComments` so these comments appear in the Swagger UI. When adding a new assembly with public API models, include its XML file too.
- Docs describe behavior and contracts (e.g. "returns Unchanged when an identical reservation exists"), not a restatement of the member name.
- Keep docs updated in the same change as the code. A change that alters behavior but not its docs is incomplete.
- Missing-docs warning (CS1591) is treated as an error in the API project so gaps are caught at build.

## Tests
- **All logic gets unit tests**: services, repositories (against a real temp SQLite file), validators, mappers, and the clock/options seams.
- **Endpoints get integration tests** with `WebApplicationFactory` and a throwaway database.
- Cover the spec's key behaviors explicitly: new vs duplicate vs changed reservation, stale update, exactly 100 allowed and the 101st rejected, window clearing, per-supplier isolation, stats counters, validation errors, and concurrent requests.
- Test names state behavior: `Ingest_IdenticalReservation_ReturnsUnchangedAndDoesNotWrite`.
- Arrange/Act/Assert, one behavior per test, deterministic time via a fake `IClock`, no real sleeping.
- Tests run and pass before a branch is pushed.

## README
- Update `README.md` when a change introduces an assumption, a decision where a Claude suggestion was changed or rejected, or a deferred item for "with more time". Keep it short.
