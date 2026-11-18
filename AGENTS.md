
# CritterOps — Agent Instructions

## Project Philosophy

CritterOps is an animal husbandry management platform developed with substantial AI assistance.

**The codebase must remain human-maintainable.**

AI agents accelerate development, but the application must remain understandable, maintainable, debuggable, and extensible by human developers without requiring AI assistance.

Optimize for:

1. Correctness and data integrity.
2. Human readability and maintainability.
3. Clear architectural boundaries.
4. Simplicity and minimal unnecessary complexity.
5. Production reliability and performance.
6. Meaningful automated regression protection.

Do not optimize for code generation speed at the expense of these principles.

## Repository Architecture

CritterOps is a monorepo containing:

- `apps/skipper-api` — ASP.NET Core backend and API.
- `apps/gilligan-web` — Next.js frontend.
- `apps/howell-sync` — External animal reference catalog synchronization.
- `packages/professor-persistence` — Shared EF Core persistence and domain models.
- `tests` — Automated tests.
- `scripts` — Development and operational utilities.
- `security` — Security scanning, reporting, and exception policy.
- `documentation` — Architecture, development, and operations documentation.

Project identities:

- Skipper — Backend/API.
- Gilligan — Frontend.
- Professor — Database and persistence.
- Ginger — UI/design system implemented within Gilligan.
- Howell Sync — External catalog synchronization worker.
- Millionare — Infrastructure and deployment.
- Mary Ann — Testing and CI.

Do not create standalone projects or packages solely to satisfy naming conventions.

Before changing an architectural boundary, inspect its existing implementation and relevant documentation.

## Engineering Principles

### Human Readability

Write code for human developers to review and maintain.

- Prefer descriptive names and explicit control flow.
- Keep methods focused and cohesive.
- Avoid dense expressions and unnecessarily clever implementations.
- Avoid compressing multiple operations onto one line.
- Make failures, state transitions, and side effects understandable.
- Do not create unnecessary classes, interfaces, wrappers, or helper methods.
- Avoid large methods containing unrelated responsibilities.
- Do not split cohesive files solely because of their line count.

Every abstraction should have a defensible purpose.

### SOLID and Rule of Three

Apply SOLID principles pragmatically.

- A module should have a clear responsibility.
- Dependencies should flow through established architectural boundaries.
- Prefer composition over unnecessary inheritance.
- Introduce interfaces for meaningful boundaries, multiple implementations, or useful testing seams.
- Do not create interfaces automatically for every class.

Apply the Rule of Three when evaluating repeated logic.

- Identify repeated domain behavior.
- Consolidate duplicated authoritative rules.
- Avoid premature abstractions based on superficial similarity.
- Preserve distinct implementations when their business semantics differ.

Never create a generic framework merely to eliminate a small amount of understandable repetition.

### Prefer Established Functionality

Prefer standard language/runtime capabilities and maintained libraries over custom infrastructure when they meaningfully reduce complexity.

Examples:

- Use established CLI parsers instead of manually processing argument arrays.
- Use `argparse` for Python CLI applications.
- Use built-in configuration and dependency injection facilities.
- Prefer established serialization and validation mechanisms.
- Use standard resource-management patterns.
- Prefer standard collection and LINQ operations where appropriate.

Before adding a dependency, evaluate:

- Whether the standard library is sufficient.
- Maintenance and version compatibility.
- Security implications.
- Developer familiarity.
- Whether the dependency actually reduces total complexity.

Do not reinvent existing platform functionality without a compelling reason.

## C# and Functional Programming

Prefer modern, idiomatic C# and functional programming principles where they improve clarity, correctness, or maintainability.

Encourage:

- Immutable records and value objects.
- Pure functions for domain transformations and calculations.
- Explicit success/failure outcomes for expected domain errors.
- Pattern matching for meaningful domain states.
- Validated construction of domain objects.
- Minimal unnecessary mutation.
- Clear separation between pure domain logic and side effects.

Avoid:

- Large objects containing unrelated nullable fields.
- Magic strings representing domain operations.
- Invalid states that can be constructed without validation.
- Exceptions for ordinary expected business outcomes.
- Unnecessary null-forgiving operators.
- Functional abstractions that make straightforward logic harder to understand.

Nullable values are appropriate when absence is legitimate.

External API DTOs must reflect actual provider contracts. Validate external data before constructing trusted internal models.

### LanguageExt Policy

LanguageExt is not a mandatory dependency.

Its adoption should be evaluated when it provides demonstrable benefits over standard C# functionality.

Potential applications include:

- Explicit Option and Either-style domain outcomes.
- Complex validation pipelines.
- Immutable domain transformations.
- Composable operations with meaningful failure states.

Do not introduce LanguageExt solely for stylistic consistency.

Do not perform large-scale conversions of existing code to functional abstractions without an approved architectural decision.

Keep API contracts, EF Core persistence models, and serialized operational state compatible with their established infrastructure.

Functional programming should simplify the domain model, not introduce unnecessary complexity.

## Skipper and Professor

### Architecture

- Keep controllers focused on HTTP concerns.
- Place domain behavior within appropriate services/domain boundaries.
- Avoid business logic duplication between controllers and services.
- Preserve Professor as the authoritative persistence layer.
- Use explicit domain validation and database constraints.
- Avoid generic repository abstractions without demonstrated need.
- Keep database queries bounded where appropriate.
- Prefer server-side filtering and projection.
- Avoid N+1 queries and unnecessary full-table materialization.
- Preserve transactional atomicity and concurrency guarantees.

### Domain Integrity

Internal CritterOps identifiers remain authoritative.

Custom Species and custom Variant assignments are supported functionality, not legacy behavior.

Animals support multiple AnimalVariantAssignment records.

Species.PreferredVariantType determines the primary variant terminology shown in Gilligan.

Do not silently remove or reinterpret domain information.

Do not remove historical database fields or reset migrations without reviewing data retention, dependencies, and migration consequences.

### Testing

Maintain strong automated regression protection for Skipper and Professor.

Prioritize:

- Domain invariants.
- Validation rules.
- PostgreSQL integration tests.
- Database constraints and transaction behavior.
- Important API contracts.
- Concurrency-sensitive operations.
- Data integrity and historical records.

Prefer realistic PostgreSQL integration testing over EF Core's in-memory provider for persistence behavior.

Avoid excessive mocking and tests coupled to implementation details.

Do not remove meaningful backend tests simply to reduce test counts.

## Howell Sync

Howell Sync manages external reference catalog synchronization.

External taxonomy providers feed:

Provider → Howell Sync → Professor → Skipper → Gilligan

Normal application operation must not depend on live external taxonomy provider availability.

Maintain:

- Stable internal CritterOps identities.
- Provider provenance and external reference IDs.
- Idempotent synchronization.
- Transactional integrity.
- Retry and recovery behavior.
- Request limits and pacing.
- Cancellation handling.
- Dry-run guarantees.
- Concurrency protections.

ICatalogProvider is a valid extension point because multiple catalog providers are planned.

### CLI and Command Design

- Use established .NET argument parsing functionality.
- Prefer typed, validated command operations.
- Separate parsing, validation, execution, and persistence concerns.
- Avoid shared command structures with excessive nullable state.
- Preserve documented exit codes and operational behavior.
- Do not serialize CLI parser objects as durable recovery state.
- Use explicit persisted operation/scope contracts where necessary.

Do not introduce elaborate command-handler frameworks or unnecessary class hierarchies.

### External Data

External provider data is untrusted.

- Preserve legitimate optional fields in transport DTOs.
- Validate required information at system boundaries.
- Use validated internal representations where beneficial.
- Do not invent missing taxonomy information.
- Preserve conservative classification behavior.
- Preserve existing import, reconciliation, and recovery semantics.

## Gilligan and Ginger

Gilligan uses Next.js and the existing Ginger design system.

Follow the Next.js-specific AGENTS.md instructions within the Gilligan application.

Do not assume framework APIs or conventions from training knowledge when the installed Next.js documentation differs.

### Frontend Architecture

- Keep page and component responsibilities clear.
- Separate complicated request lifecycles from rendering when beneficial.
- Avoid unnecessary global state libraries.
- Reuse existing Ginger components and design tokens.
- Preserve accessibility and keyboard interaction behavior.
- Avoid duplicated API transformation and mapping logic.
- Keep asynchronous operations safe against stale responses.
- Prefer bounded server-side queries over loading entire datasets.

Do not create a separate design-system package without a demonstrated need.

### Frontend Testing

Gilligan should maintain a small, behavior-focused automated test suite.

Do not create unit tests for:

- Purely presentational components.
- Static markup.
- Trivial layout changes.
- Simple cards, skeletons, or decorative components.
- CSS implementation details.
- Internal component structure without meaningful behavioral impact.

Prioritize focused tests for:

- API communication and request construction.
- Important failure and recovery behavior.
- Critical form submission contracts.
- Data preservation and custom-value handling.
- Complex asynchronous request behavior.
- Meaningful accessibility and keyboard interactions.

Avoid duplicated coverage across hooks, components, and API clients.

Do not introduce broad browser end-to-end suites by default.

Do not require new component tests simply because a component was created.

Testing should protect meaningful behavior rather than satisfy arbitrary coverage percentages.

## Scripts and Developer Tooling

Scripts must remain understandable to human maintainers.

- Prefer short shell scripts for simple orchestration.
- Use Python for structured logic when it improves clarity.
- Use argparse for Python CLI argument parsing.
- Use appropriate standard-library facilities for subprocesses, paths, and resources.
- Avoid deeply nested shell expressions and obscure argument manipulation.
- Preserve exit codes, error reporting, and cleanup behavior.
- Avoid duplicating functionality between scripts.
- Keep script locations consistent with repository conventions.

Security-related scripts must preserve fail-closed behavior and evidence generation.

Do not weaken security guarantees to simplify code.

## Documentation

Documentation should describe the current supported architecture and operational behavior.

Maintain authoritative documentation for:

- Architecture and domain design.
- Developer setup.
- Testing and CI.
- Operations and deployment.
- Security.
- Accepted architectural decisions.

Do not generate implementation reports for every completed ticket.

Pull requests should describe implementation changes.

Architecture documentation should describe how the maintained system works.

Update documentation when supported contracts or architecture change.

Remove or archive obsolete documentation after preserving unique historical decisions.

Do not document speculative behavior as implemented functionality.

## Technical Debt and Compatibility

CritterOps is pre-production.

Prefer removing obsolete implementations over preserving unnecessary backward compatibility.

When replacing behavior:

1. Identify the authoritative implementation.
2. Identify active consumers and persistence dependencies.
3. Establish appropriate regression coverage.
4. Replace the obsolete implementation.
5. Remove superseded adapters, mappings, tests, and documentation.
6. Preserve data and recovery compatibility where genuinely required.

Do not maintain two implementations of the same domain behavior without a justified reason.

Do not delete migrations, persisted state, or retained recovery history merely because the application is pre-production.

## Performance and Reliability

CritterOps is intended for production use.

- Bound growing queries and external requests.
- Preserve deterministic pagination and ordering.
- Avoid avoidable allocations and unnecessary materialization.
- Prefer efficient database projections.
- Measure performance concerns before introducing caching or optimization frameworks.
- Ensure concurrent operations preserve domain invariants.
- Handle cancellation and partial failures explicitly.
- Avoid hiding failures or producing misleading success responses.

Do not sacrifice readability for speculative micro-optimizations.

## AI Development Workflow

Before implementing a feature:

1. Inspect the relevant existing implementation.
2. Identify authoritative domain and architectural boundaries.
3. Reuse established utilities and patterns.
4. Identify affected consumers and tests.
5. Consider whether new abstractions or dependencies are actually necessary.

During implementation:

- Keep changes focused on the requested behavior.
- Prefer readable, cohesive code.
- Avoid unrelated refactoring.
- Remove unnecessary duplication introduced by the change.
- Avoid compatibility layers without demonstrated need.
- Do not introduce speculative features.
- Preserve supported domain behavior.

After implementation:

- Run relevant builds, linting, and tests.
- Verify important regression behavior.
- Review new code for unnecessary complexity.
- Update authoritative documentation when appropriate.
- Remove obsolete code made redundant by the change.
- Report validation performed and any limitations.

Do not claim tests passed unless they were actually executed successfully.

## Code Review Rules

When reviewing changes, flag:

- Duplicated authoritative business logic.
- Unnecessary manual implementations of existing library functionality.
- Excessive nullable or invalid command/domain states.
- Unnecessary abstraction layers.
- Controllers containing unrelated business responsibilities.
- Unbounded database queries.
- Missing concurrency protection for exactly-once operations.
- Incorrect or unsafe persistence changes.
- Tests focused on implementation details rather than meaningful behavior.
- New presentational component unit tests.
- Obsolete compatibility code.
- Stale or contradictory documentation.
- Security protections weakened without equivalent replacement.

Do not recommend refactoring merely because a file is large or an interface has one implementation.

Every substantial maintainability recommendation should explain the actual problem and expected benefit.

## Final Principle

**CritterOps is developed with AI but maintained by humans.**

Every implementation decision should make the application easier to understand, operate, debug, and extend over its lifetime.
