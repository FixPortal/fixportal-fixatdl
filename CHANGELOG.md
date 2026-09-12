# Changelog

Notable changes to `FixPortal.FixAtdl`. Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project uses
[semantic versioning](https://semver.org/spec/v2.0.0.html).

Dates are the release-tag date, in UTC. Entries are consumer-facing: build,
CI, and test-infrastructure commits are omitted unless they change what a
consumer sees. No `1.0.2`–`1.0.4` release was tagged.

## [Unreleased]

### Added

- `docs/usage.md` — consumer guide covering loading, value setting, validation,
  FIX output, and the exception surface.
- This changelog.

### Fixed

- README no longer claims the public surface is locked by `PublicAPI.Shipped.txt`;
  that analyzer was removed before `1.0.5` (see below).

## [1.1.2] — 2026-09-12

### Fixed

- Release pipeline uses the NuGet policy creator for OIDC login. Packaging only;
  no library change.

## [1.1.1] — 2026-09-12

### Changed

- Packages publish to NuGet.org under the FixPortal organization.

## [1.1.0] — 2026-09-12

### Added

- `StrategiesReader` accepts a caller-supplied `XmlSchemaSet` for XML Schema
  validation before deserialization. The FIXatdl 1.1 XSD is FIX Protocol
  Limited's licensed schema and is not vendored here; supply your own.
- Host-registered custom `Parameter` `xsi:type` extensions, so a vendor
  parameter type whose CLR type lives in the caller's assembly can be resolved.
- Type-directed `FIX_` field comparisons, driven by a FIX field type dictionary
  rather than string comparison.
- Declared UTC leap seconds are normalised rather than rejected.
- Offset-bearing `Clock` `initValue` resolves without a `localMktTz`.

### Fixed

- Offset-bearing timestamp bounds anchor against UTC, not zone-local time.
- Bare-hour offset suffixes are recognised as offset-anchored.
- An offset-time `initValue` anchors "today" in its own offset, not UTC.
- `ClrType` is validated eagerly, and a duplicate registration reports a clear
  error instead of failing later.
- A caller-supplied `XmlSchemaSet` is compiled eagerly, avoiding a race.

## [1.0.6] — 2026-09-12

### Added

- `StrategyParametersGrpEmitter` emits the FIX StrategyParametersGrp sequence
  (tags 957–960) from core, for hosts using `Tag957Support` transport instead of
  or alongside direct per-parameter FIX tags.

## [1.0.5] — 2026-09-12

### Added

- `docs/conformance.md` — assessed FIXatdl 1.1 scope and limits against the
  December 2010 errata, with the shared adapter scenarios.

### Fixed

- Selection identity and typed initialization edge cases.
- FIXatdl control mappings and parameter constraints.
- Typed edit comparisons and required operands.

### Removed

- `PublicApiAnalyzers` and the `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`
  tracking files (2026-07-04). The public surface is no longer guarded by a
  build-breaking analyzer; semantic versioning is the contract.

## [1.0.1] — 2026-06-03

### Changed

- Cleared IDE and style warnings; CA1062 and CA2007 policy locked.

## [1.0.0] — 2026-06-03

First stable release of the modernised fork.

### Added

- FIXatdl 1.1 parsing, validation, and FIX-tag emission targeting `net10.0`.

### Changed

- Namespace `Atdl4net.*` → `FixPortal.FixAtdl.*`.
- `Common.Logging` → `Microsoft.Extensions.Logging.Abstractions`.
- `System.Configuration` glue replaced with the `FixAtdlOptions` POCO.
- Nullable reference types enabled throughout.

### Removed

- The upstream WPF rendering layer; the library is UI-agnostic. Editable
  desktop and browser forms live in the
  [WPF](https://github.com/FixPortal/fixportal-fixatdl-wpf) and
  [React](https://github.com/FixPortal/fixportal-fixatdl-react) adapters.

## [0.1.0] — 2026-05-24

First packaged fork of [Atdl4net](https://github.com/atdl4net/atdl4net),
pre-release.

[Unreleased]: https://github.com/FixPortal/fixportal-fixatdl/compare/v1.1.2...HEAD
[1.1.2]: https://github.com/FixPortal/fixportal-fixatdl/compare/v1.1.1...v1.1.2
[1.1.1]: https://github.com/FixPortal/fixportal-fixatdl/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/FixPortal/fixportal-fixatdl/compare/v1.0.6...v1.1.0
[1.0.6]: https://github.com/FixPortal/fixportal-fixatdl/compare/v1.0.5...v1.0.6
[1.0.5]: https://github.com/FixPortal/fixportal-fixatdl/compare/v1.0.1...v1.0.5
[1.0.1]: https://github.com/FixPortal/fixportal-fixatdl/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/FixPortal/fixportal-fixatdl/compare/v0.1.0...v1.0.0
[0.1.0]: https://github.com/FixPortal/fixportal-fixatdl/releases/tag/v0.1.0
