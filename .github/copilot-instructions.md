# Copilot instructions for FixPortal.FixAtdl

## Build, test, and verify

- Restore: `dotnet restore FixPortal.FixAtdl.sln`
- Build: `dotnet build FixPortal.FixAtdl.sln -c Release --no-restore`
- Test: `dotnet test FixPortal.FixAtdl.sln -c Release --no-build`
- Single test: `dotnet test tests\FixPortal.FixAtdl.Tests\FixPortal.FixAtdl.Tests.csproj --filter "FullyQualifiedName~FixPortal.FixAtdl.Tests.Parsing.StrategiesParserTests.Parse_twap_fixture_yields_one_strategy_named_TWAP"`
- Mutation pilot: `dotnet tool restore` then `dotnet stryker --config-file stryker-config.json`
- Formatting check: `dotnet format --verify-no-changes`

## Big picture

This is a headless .NET 10 library for parsing, validating, and emitting FIX-tag values from FIXatdl v1.1 strategy XML.

- `src/FixPortal.FixAtdl/Xml` owns XML loading and deserialization. `StrategiesReader` loads XML, deserializes `Strategies_t`, and resolves cross-references.
- `src/FixPortal.FixAtdl/Model` mirrors the FIXatdl schema with strongly typed `*_t` model classes, collections, enums, and support interfaces.
- `src/FixPortal.FixAtdl/Fix` handles FIX wire-value parsing and emission, including `FixMessage`, `FixTagValuesCollection`, and parameter-to-tag mapping.
- `src/FixPortal.FixAtdl/Validation` evaluates edit/state-rule behavior and value conversion.
- `src/FixPortal.FixAtdl/Configuration`, `Diagnostics`, `Resources`, and `Utility` provide options, exceptions, localized messages, and helpers.
- `tests/FixPortal.FixAtdl.Tests` mirrors the source layout and uses XML fixtures for parsing and conformance coverage.

## Key conventions

- Keep the `*_t` type names. They are part of the FIXatdl XML contract, not cosmetic suffixes.
- Preserve the `// FP Enhancement: <date> -- <reason>` banner on files inherited from upstream when you modify them.
- Use NodaTime for domain date/time values; keep BCL date/time types at I/O boundaries.
- `FixMessage` and related FIX types are intentionally low-level and order-sensitive; do not assume FIX-spec ordering is handled elsewhere.
- `ToString()` returning `null` is meaningful in this codebase for omitting optional FIX wire values.
- Follow the repo's C# conventions from `.editorconfig`: file-scoped namespaces, nullable enabled, warnings as errors, and style rules enforced in build.
- Tests use xUnit v3, `AwesomeAssertions` (`.Should()`), and NSubstitute. Test helpers live under `tests/FixPortal.FixAtdl.Tests/TestInfrastructure`.
- Use PowerShell-friendly commands and Windows paths when working in this repo.
