# Batch 8 — Adversarial Audit Fixes — Design Spec

**Date:** 2026-06-02
**Status:** Completed
**Scope:** `FixPortal.FixAtdl` core library + tests. Branch `reviewer-findings-batch8`.

---

## Goals

1. **Address High and Medium Findings** identified in the adversarial audit report `E:\Documents\Obsidian Vault\Claude\Adversarial Review\fixportal-fixatdl\20260601T213106Z\report.md` (Completed).
2. **Address Selected Low Findings** to resolve Boolean string discrepancies, SOH exposure, undefined enum parses, format safety, and shared mutable state hazards.
3. **Verify via Unit Testing** and ensure compilation flags remain clean.

---

## 1. High Findings [H1] to [H5]

### 1.1 [H1] EditRef-only StateRule NRE
- **Problem**: When a `StateRule` contains only an `<EditRef>` (so `Edit` is null), `ReadOnlyControlCollection.UpdateRelatedHelperControls` dereferences `stateRule.Edit` and throws an `NullReferenceException`.
- **Design**: Introduce a null-conditional or pattern matching guard checking `stateRule.Edit is not { } edit` before evaluating the operator. If `edit` is null, skip helper control updates for that state rule.

### 1.2 [H2] Mixed-Numeric/Cross-CLR-Type Edit Comparisons
- **Problem**: Mismatched CLR types (e.g. comparing native `int` from `Int_t.GetNativeValue` to a decimalised `FIX_` field or string) cause `IComparable.CompareTo` to throw raw `ArgumentException` or fail silently.
- **Design**: Add an internal `NormaliseValue` utility to `Edit_t.cs` that converts all basic native numeric types (`int`, `uint`, `double`, `float`, `decimal`, etc.) and numeric strings to `decimal`. Apply `NormaliseValue` to both operands in `AreEqual` and `EvaluateInequalityComparison`.
- Throw a clean `InvalidOperationException` (with `ErrorMessages.UnsupportedComparisonOperation`) if the normalised types still mismatch, instead of allowing raw BCL exceptions to escape.

### 1.3 [H3] RepeatingGroup Fail-Fast
- **Problem**: `RepeatingGroup` deserialization is not supported but silently drops leg/list parameters without errors.
- **Design**: In `StrategiesReader.LoadStrategies`, check the XML document for any elements with `LocalName == "RepeatingGroup"`. If any are found, immediately fail fast by throwing a `FixAtdlException` with message `"RepeatingGroup elements are not supported."`

### 1.4 [H4] Clock_t Timezone/Unspecified Shifts
- **Problem**: `Clock_t` round-trips silently shift the instant by timezone offset when `Unspecified` `DateTime` is passed.
- **Design**: 
  - Change `ToInstant(DateTime)` to throw an `ArgumentException` if the `DateTimeKind` is `Unspecified`, preventing timezone-ambiguity bugs.
  - In `GetCurrentValue()`, if `LocalMktTz` is set but the zone cannot be resolved by `TimeZoneProvider`, throw an `InvalidFieldValueException` instead of falling back silently to UTC.

### 1.5 [H5] UTCTimestamp_t / UTCTimeOnly_t Ms Truncation
- **Problem**: Sub-second precision (milliseconds) is silently truncated on serialization because `UTCDateTimeTypeBase.ConvertToWireValueFormat` always defaults to the first format in the list (without ms).
- **Design**: Override or update `UTCDateTimeTypeBase.ConvertToWireValueFormat` to dynamically pick the millisecond-bearing format if the value ticks are not a whole second multiple (i.e. `ticks % TimeSpan.TicksPerSecond != 0`) and the type defines more than one format.

---

## 2. Selected Medium Findings Design

### 2.1 [M1] ReadOnlyControlCollection Replace Duplicates
- **Problem**: `ReadOnlyControlCollection.Replace` case re-keys a control under a new ID without duplicate ID checking, potentially evicting another live control.
- **Design**: In `ReadOnlyControlCollection.cs:71-82` (inside the `Replace` / `SetItem` channel), throw `ArgumentException` or a domain `DuplicateKeyException` if `newControl.Id` already maps to a different control.

### 2.2 [M2] Tenor Cross-Unit Ordering
- **Problem**: `Tenor.Compare` collapses distinct units (e.g. `D30` and `M1`) to equality because it compares only `ApproximateDays`. `ApproximateDays` also returns `NaN` for `Invalid`.
- **Design**: Add a non-zero tie-breaker in `Tenor.Compare` using the unit type and offsets. Guard the `Invalid` tenor case to fail fast.

### 2.3 [M3] MonthYear Suffix Ordering
- **Problem**: `MonthYear.Compare` contradicts `operator==` when day vs week suffixes are mixed (e.g. `202601 day=7` vs `202601w1 week=1`).
- **Design**: Tie-break `MonthYear.Compare` on the suffix fields (day, week, ordinal) to establish a deterministic total order.

### 2.4 [M4] DateTimeTypeBase.SetBound Stale Constraints
- **Problem**: Re-setting a bound as time-only or datetime leaves the other slot active.
- **Design**: In `DateTimeTypeBase.SetBound`, clear the alternative constraint slot (e.g. clear time-only bounds when setting a datetime bound and vice versa).

### 2.5 [M5] Percentage Const scaling
- **Problem**: Percentage const value is divided by 100 in `Percentage_t.cs`, resulting in 100x mis-scaling compared to the non-const value path.
- **Design**: In `Percentage_t.cs`, treat `ConstValue` identically to `_value` and do not divide by 100 in `GetNativeValue`.

### 2.6 [M6] Float/Percentage Precision Validation
- **Problem**: Negative or extremely large `Precision` values in XML cause `Math.Round` to throw `ArgumentOutOfRangeException`.
- **Design**: In `Float_t.cs`/`Percentage_t.cs`, validate `Precision` on assignment (ensure it is between `0` and `28`) or catch `ArgumentOutOfRangeException` and throw `InvalidFieldValueException`.

### 2.7 [M7] EnumTypeBase unset enum `{NULL}` selection
- **Problem**: Unset enum parameter selection falls back to the `{NULL}` enum pair.
- **Design**: Return an empty `EnumState` when `ConstValue ?? _value` is null, avoiding lookup of `{NULL}`.

---

## 3. Recommended Cosmetic/Low Fixes

### 3.1 [H6] GetWireValue Nullability
- **Design**: Annotate `IParameter.WireValue` and `IParameterType.GetWireValue` as nullable (`string?`). Remove `null!` suppressors in the `ConvertToWireValueFormat` overrides.
