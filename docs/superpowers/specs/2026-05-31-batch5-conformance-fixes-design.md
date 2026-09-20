# Batch 5 — Conformance Fixes — Design

**Date:** 2026-05-31
**Status:** Draft (pending user review) — design for the fixes catalogued in
`docs/batch-5-conformance-review.md`.
**Scope:** `FixPortal.FixAtdl` library + its test project. Branch
`reviewer-findings-batch5`.

## Goals

Correctness, robustness, infallibility against real broker ATDL. Fix all actionable
findings (C1, C2, H1–H4, M1–M4), add the two production specs as conformance fixtures,
and lock the behaviour with tests. No gratuitous public-surface churn; `v0.1.0` is parked
and unpublished, so the few necessary surface changes are acceptable.

---

## 1. Time & timezone (C1 + C2 + M1) — the core change

### 1.1 The domain

A `Clock_t` control carries a time (`initValue`, often **time-only** e.g. `08:00:00`)
expressed in `localMktTz` (an **IANA zone**, e.g. `Europe/Berlin`). It feeds a
`UTCTimestamp_t` parameter (FIX tag 7113/7114), whose wire value must be **UTC**. So
`localMktTz` is the bridge: *local market time → UTC on the wire*, DST-aware.

Two representations must stay distinct:

- **Display value** (what a UI shows, what `GetCurrentValue()` returns): the *local market*
  time — 08:00 Berlin.
- **Wire value** (what the `UTCTimestamp_t` parameter emits, via
  `IControlConvertible.ToDateTime(targetParameter, provider)`): the *UTC* instant.

Because the parameter cannot know `localMktTz`, the local→UTC conversion **must originate in
`Clock_t`**.

### 1.2 NodaTime adoption (first in this library)

- Add `NodaTime` (latest 3.x) to `Directory.Packages.props` runtime group + reference from
  `FixPortal.FixAtdl.csproj`. Add `NodaTime.Testing` to the test group.
- BCL↔NodaTime seam is confined to `Clock_t` (+ a small time-conversion helper). The public
  value surface stays BCL `DateTime?` (controls return `DateTime?`; parameters format BCL
  `DateTime`). NodaTime types do **not** leak onto the public model surface.

### 1.3 Injected clock + zone provider (replaces `TimeProvider`)

On `Clock_t`, replace `public TimeProvider TimeProvider { get; set; }` with:

```csharp
public IClock Clock { get; set; } = SystemClock.Instance;
public IDateTimeZoneProvider TimeZoneProvider { get; set; } = DateTimeZoneProviders.Tzdb;
```

(Settable, defaulted — set after reflective construction; tests assign `FakeClock` and the
default Tzdb provider.) `Microsoft.Extensions.TimeProvider.Testing` is removed if no longer
used elsewhere (only `Clock_t` used it).

### 1.4 Parsing `initValue`

`initValue` may be **time-only** (`HH:mm:ss`) or a full datetime. We must not inject a
spurious date (C2). Parse into NodaTime:

- Time-only → `LocalTime`.
- Date+time → `LocalDateTime`.

A new internal parser (in `Fix/` or a `Time/` helper) replaces the `FixDateTime`/`DateTime`
round-trip for the clock path, using NodaTime `LocalTimePattern` / `LocalDateTimePattern`
with the FIX formats. **`Clock_t.InitValue` changes type** from `DateTime?` to a small
internal representation (a `LocalTime?`/`LocalDateTime?` discriminated holder, or two
nullable fields). This is an internal/parse-surface change; document it.

> **Decided:** change the type outright (no `DateTime?` shim) — it is parse-layer state, parked
> release, and a `DateTime?` cannot faithfully hold "time-only with no date".

### 1.5 Resolution (`LoadDefaultFromInitValue`)

```
zone        = TimeZoneProvider[localMktTz]                       // DateTimeZone
nowInstant  = Clock.GetCurrentInstant()
marketToday = nowInstant.InZone(zone).Date                       // LocalDate
localDt     = time-only ? marketToday + LocalTime : LocalDateTime
zonedDt     = zone.ResolveLocal(localDt, Resolvers.LenientResolver)   // DST gap/overlap safe
initInstant = zonedDt.ToInstant()
```

- **initValueMode 0 (default):** display value = `localDt` (local); wire value (lazily, at
  `ToDateTime`) = `initInstant` → UTC.
- **initValueMode 1** ("use current time if initValue has passed"): compare `initInstant` to
  `nowInstant`; if `nowInstant > initInstant`, the effective value is "now" (`nowInstant`),
  else `initInstant`. Comparison is on **instants** — timezone-correct, replacing the broken
  host-local `DateTime` comparison.
- **`localMktTz` missing while `initValue` present:** the FIXatdl spec says `localMktTz` is
  required when `initValue` is supplied. **Decided:** throw a clear validation error (fail fast)
  rather than silently assuming a zone and emitting a wrong UTC instant.

### 1.6 Wire conversion boundary

`Clock_t.ToDateTime(targetParameter, provider)` returns the value **converted to UTC**
(`Kind = Utc`) using the stored zone, so the `UTCTimestamp_t` path emits the correct UTC
instant. `GetCurrentValue()` continues to return the **local** representation for display.

### 1.7 M1 — UTC type parse styles

Independently fix `UTCDateTimeTypeBase.ConvertFromWireValueFormat` to add
`DateTimeStyles.AdjustToUniversal` (matching `TZTimestamp_t`) so a `Local`-kind value is
normalised, and retire the now-dead `WireParseStyles` override for the UTC family (or route
through it). Values reaching `GetAdjustedValue` with `Kind = Utc` (the C1 fix output) pass
through unchanged.

### 1.8 C2 — time-only bound on `UTCTimestamp_t`

`maxValue="23:59:59"` is a **time-of-day** bound. Model time-only parameter bounds as a
time-of-day and compare only the time component (or document that `UTCTimestamp_t` bounds
expressed time-only constrain the time component). Remove the injected-date contamination by
not parsing time-only values into date-bearing `DateTime`s in the bound path.

---

## 2. Edit evaluation fixes

### 2.1 H1 — EX/NX for list controls

Add a presence predicate to `EnumState` (e.g. `bool HasSelection` / `bool IsEmpty` — true when
no enum is enabled). In `Edit_t.EvaluateExists`, treat an `EnumState` operand as absent when it
has no selection (in addition to the existing `null`/`""` checks). Keep current behaviour for
scalar/text/clock controls (already correct — they return `null` when unset).

### 2.2 H2 — null RHS inequality is indeterminate

In `EvaluateInequalityComparison`, handle a null RHS symmetrically to the existing null-LHS
short-circuit (`return false`), instead of falling through to `lhs.CompareTo(null)`. A missing
comparison operand yields `false` (indeterminate), not a spurious ordering.

### 2.3 M2 — both `value` and `field2` set

Reject "both set" at resolve/parse time with a clear error (they are mutually exclusive forms),
or — if rejection is too strict for lenient real-world parsing — document and assert the
`field2`-wins precedence for the two-field form. **Decided:** reject "both set" at parse/resolve
with a precise message (they are mutually exclusive forms).

### 2.4 M3 — unset binary control vs `EQ "false"`

Ensure binary controls (`CheckBox_t`/`RadioButton_t`) initialise to a concrete `false` (not
`null`) when no `initValue`/FIX field applies, so a StateRule keyed on `EQ "false"` fires
deterministically. Verify against `InitializableControl.LoadInitValue` and binary
`LoadDefaultFromInitValue`. Add a regression test for the broker-431 `EnableStartTime` pattern.

---

## 3. Parse-fidelity & semantics fixes

### 3.1 H3 — `EnumPair@index`

**Decided:** capture it. `index` is an **optional extension attribute** (standard FIXatdl 1.1
`EnumPair` is `enumID`+`wireValue`; `index` is a vendor ordering hint). For lossless fidelity and
to support the conformance fixtures, add `int? Index` to `EnumPair_t` and map `index` in the
schema definition (optional — absence is fine). Documented as a captured extension; it does not
affect wire output.

### 3.2 H4 — `definedByFIX`

`definedByFIX="true"` marks a parameter as a redefinition of a standard FIX tag. It does not
change the wire value. **Decided:** keep it **informational/inert** but make that a
*deliberate, documented* property contract on `Parameter_t.DefinedByFix` (XML-doc the semantics
and that no validation gate is applied), rather than an accidental dead field. We will NOT invent
a validation gate that could alter correct wire output without evidence it is required.

### 3.3 M4 — precision rounding mode

Confirm `MidpointRounding.AwayFromZero` is the intended convention; document it on the precision
path. No behaviour change unless a different mode is mandated.

---

## 4. Fixtures & tests

- Add the two distinct production specs as fixtures under
  `tests/FixPortal.FixAtdl.Tests/Fixtures/` (e.g. `broker-82.xml`, `broker-431.xml`), extracted
  and pretty-printed. **Sanitisation check:** these contain `YOUR_OMS_NAME`/`YOUR_OMS_VERSION`
  placeholders and broker algo metadata — confirm nothing genuinely sensitive (credentials,
  internal-only identifiers) is committed; redact if needed.
- **Conformance tests** (parse + assert; the library is a reader/model, not an XML serialiser):
  load each fixture, assert it parses without error, and assert the previously-broken constructs
  now behave correctly:
  - C1: a `Clock_t` with `localMktTz="Europe/Berlin"` + `initValue="08:00:00"` yields the correct
    **UTC** wire value (06:00Z winter / 07:00Z summer) via the target `UTCTimestamp_t` — drive
    "today" with `FakeClock` across a DST boundary.
  - C2: `maxValue="23:59:59"` validates as a time-of-day, no date contamination.
  - H1: an unselected `DropDownList_t` reports `NX` true / `EX` false.
  - H2: an inequality against a missing FIX field evaluates `false`.
  - H3: `EnumPair@index` is captured.
- Unit tests for each fix (xUnit v3, AwesomeAssertions, NodaTime `FakeClock`). Keep the existing
  suite green.

## 5. Backward compatibility

- `Clock_t`: `TimeProvider` → `IClock`/`IDateTimeZoneProvider`; `InitValue` type change. These are
  the only surface changes, justified and on a parked release.
- All parameter/value-type public surfaces unchanged. `Parameter_t.DefinedByFix`,
  `EnumPair_t.Index` are additive.

## 6. Out of scope

- N2 (applying StateRule `enabled`/`visible`/`value` to controls inside the library) — by design,
  the consuming UI applies them; documented, not changed here.
- Adversarial-review panel (Phase 3) remains deferred.

## 7. Resolved decisions (user-approved 2026-05-31)

1. `Clock_t.InitValue` — **change the type** to a NodaTime time-only/datetime holder; no `DateTime?` shim (§1.4).
2. `localMktTz` missing while `initValue` present — **throw / fail fast** (§1.5).
3. M2 both `value` and `field2` — **reject with a clear error** (§2.3).
4. H4 `definedByFIX` — **inert + documented contract**, no validation gate (§3.2).
5. H3 `EnumPair@index` — **capture** as `int? Index` for fidelity (§3.1).
