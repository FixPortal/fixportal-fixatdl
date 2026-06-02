---
title: FixPortal.FixAtdl Batch 8 — Review Report
date: 2026-06-02
reviewer: Claude Sonnet 4.6 (code-review skill, high + full-bore workflow)
branch: reviewer-findings-batch8
method: 7-angle standard pass → 12-finder workflow (62 agents, 70 raw candidates, 38 verified surviving)
status: resolved
---

# Batch 8 — Code Review Report

Review of Antigravity's fixes on branch `reviewer-findings-batch8` against the consolidated adversarial audit (`E:\Documents\Obsidian Vault\Claude\Adversarial Review\fixportal-fixatdl\20260601T213106Z\`).

**Overall verdict:** The fixes are directionally correct — all original audit findings were addressed at the right level. Twelve correctness bugs were found in the implementation, three of which are pre-existing defects exposed by the review rather than introduced by this branch.

---

## Findings

### In-diff bugs (introduced or exposed by batch 8 changes)

---

#### F1 — Boolean_t.cs: FalseWireValue setter does not guard against default-value collision
**File:** `src/FixPortal.FixAtdl/Model/Types/Boolean_t.cs` ~line 40  
**Severity:** High  

**Problem:** The `FalseWireValue` setter checks `value == _trueWireValue` (the backing field). When `TrueWireValue` has never been explicitly assigned, `_trueWireValue` is `null`. Setting `FalseWireValue = "Y"` (the default true wire value) passes the check silently — `"Y" == null` is false. The symmetric gap exists in the `TrueWireValue` setter when `FalseWireValue` is unset.

**Result:** `ConvertToWireValueFormat(true)` returns `TrueWireValue ?? DefaultTrueValue = "Y"` and `ConvertToWireValueFormat(false)` returns `FalseWireValue = "Y"`. Both states emit `"Y"` on the wire; a FIX consumer cannot distinguish true from false. No exception is raised.

**Reachable via:** XML deserialization setting `falseWireValue="Y"` without setting `trueWireValue`.

**Fix:** Validate against the effective value (`TrueWireValue ?? DefaultTrueValue` / `FalseWireValue ?? DefaultFalseValue`) rather than the backing field. Or validate at the point of use in `ConvertToWireValueFormat` and throw there.

---

#### F2 — AtdlReferenceType.cs: WireValue = "" silently accepted; getter returns null → FIX tag omitted
**File:** `src/FixPortal.FixAtdl/Model/Types/Support/AtdlReferenceType.cs` ~line 149  
**Severity:** High  

**Problem:** `String_t.ConvertFromWireValueFormat` is an identity function — it returns the input string unchanged. So `parameter.WireValue = ""` stores an empty string without error and `IsSet` becomes true. However, `String_t.ConvertToWireValueFormat("")` returns `null` (`string.IsNullOrEmpty(value) ? null : value`). A subsequent `GetOutputValues` call sees `WireValue == null` and skips the parameter entirely — the FIX tag is never emitted.

**Result:** Empty-string assignment succeeds silently. The parameter appears set (`IsSet = true`) but produces no FIX output. Silent data loss.

**Fix:** Either reject empty string in `SetWireValue` (throw `InvalidFieldValueException`) or make `ConvertFromWireValueFormat` return `null` for empty string so `IsSet` stays false. The asymmetry between setter and getter must be resolved.

---

#### F3 — Edit_t.cs: NaN/Infinity comparison incorrect due to bare catch in NormaliseValue
**File:** `src/FixPortal.FixAtdl/Model/Elements/Edit_t.cs` ~line 476  
**Severity:** Medium  

**Problem:** `NormaliseValue` catches all exceptions from `Convert.ToDecimal` and returns the original value unchanged. For `double.NaN` or `double.PositiveInfinity`, `Convert.ToDecimal` throws `OverflowException`; the catch returns the original `double`. When both operands are `NaN`, they both survive as `double`, types match, and `double.NaN.CompareTo(double.NaN)` returns 0 — making `NaN >= NaN` and `NaN <= NaN` evaluate to `true`, which is incorrect per IEEE 754.

When only one operand fails conversion (e.g. `double.PositiveInfinity` vs a normal `double`), the type-mismatch guard fires with a confusing `InvalidOperationException` instead of a clear domain error.

**Fix:** Explicitly guard against `double.IsNaN` and `double.IsInfinity` in `NormaliseValue` (or in `IsNumericType`) and throw `InvalidOperationException` with a clear message.

---

#### F4 — Edit_t.cs: Data_t inequality comparison silently returns false instead of domain error
**File:** `src/FixPortal.FixAtdl/Model/Elements/Edit_t.cs` ~line 382  
**Severity:** Medium  

**Problem:** `CheckForUnsupportedComparisons` (which raises `InvalidOperationException` for `Data_t`/`char[]` comparisons) is called from `EvaluateEquality` but NOT from `EvaluateInequalityComparison`. For GT/LT/GTE/LTE on a `Data_t` field, `lhs as IComparable` is `null` (char[] does not implement IComparable). The existing null guard at the top of `EvaluateInequalityComparison` returns `false` silently.

**Result:** A `StrategyEdit` with `operator="GT"` on a `Data_t` field silently evaluates to `false` regardless of values. The same edit with `operator="EQ"` correctly raises an exception.

**Fix:** Call `CheckForUnsupportedComparisons(lhs, rhs)` at the top of `EvaluateInequalityComparison`, the same as `EvaluateEquality` does.

---

#### F5 — DateTimeTypeBase.cs: ToString(IFormatProvider) hardcodes formats[0], dropping sub-second precision
**File:** `src/FixPortal.FixAtdl/Model/Types/Support/DateTimeTypeBase.cs` ~line 174  
**Severity:** Medium  

**Problem:** `DateTimeTypeBase.ToString(IFormatProvider?)` calls `GetDateTimeFormatStrings()[0]` directly without going through `ConvertToWireValueFormat`. The `UTCDateTimeTypeBase.ConvertToWireValueFormat` override correctly picks `formats[1]` for sub-second values, but the base `ToString` bypasses it entirely.

**Result:** `parameter.WireValue` correctly emits `"20260101-09:30:00.123"` (wire path uses the override), but `control.ToString(targetParameter)` returns `"20260101-09:30:00"` (display path uses the base method with `formats[0]`). A control that round-trips the display value will silently corrupt a millisecond timestamp.

**Fix:** Override `ToString(IFormatProvider?)` in `UTCDateTimeTypeBase` with the same millisecond-aware format selection, or extract the format-selection logic into a shared virtual method called by both.

---

#### F6 — Percentage_t.cs: Precision rounding applied at wrong scale when MultiplyBy100 = false
**File:** `src/FixPortal.FixAtdl/Model/Types/Percentage_t.cs` ~line 97  
**Severity:** Medium  

**Problem:** In `ConvertToWireValueFormat`, when `MultiplyBy100 == false`, `adjustedValue = value` (the raw native fraction, e.g. `0.0025` for 0.25%). `Round(adjustedValue, Precision.Value)` then rounds `0.0025` to `Precision` decimal places. With `Precision=2`, `Round(0.0025, 2) = 0.00`, emitting `"0"` instead of `"0.0025"`.

**Result:** Percentage parameters with `MultiplyBy100=false` and `Precision` set are silently rounded to zero for small values. The strategy author setting `Precision=2` expects two decimal places on the transmitted percentage string, but the rounding is applied to the fraction.

**Fix:** Apply `Precision` to `adjustedValue` after scaling: round the wire-representation number, not the native fraction. When `MultiplyBy100=false`, the wire value equals the native value, so `Precision` should be applied to `value` directly — but this is already the case; the real issue is that the semantics of `Precision` on a non-multiplied `Percentage_t` are ambiguous. Clarify in the spec and document the intended behaviour.

---

#### F7 — Tenor.cs: IComparable.CompareTo throws ArgumentException for default(Tenor), violating the contract
**File:** `src/FixPortal.FixAtdl/Model/Types/Support/Tenor.cs` ~line 161  
**Severity:** Medium  

**Problem:** `Tenor.Compare` throws `ArgumentException` when either operand has `TenorType == Invalid`. `IComparable.CompareTo` is only supposed to throw `ArgumentException` when the argument is of the wrong type — throwing for a valid (if sentinel) struct value violates the contract. Any framework code that uses `IComparable` (sorting, binary search, `List<Tenor>`) will break if a `default(Tenor)` is present.

**Additional asymmetry:** `operator==` correctly returns `true` for two `default(Tenor)` values, but `operator<` / `CompareTo` throw — equality and ordering are inconsistent for the Invalid state.

**Fix:** Either return a defined ordering for `Invalid` (e.g. `Invalid` is always less than any valid tenor, and two `Invalid` values are equal — consistent with the `operator==` behaviour), or document that `default(Tenor)` is not a valid runtime value and guard at construction sites.

---

#### F8 — StrategiesReader.cs: RepeatingGroup guard is namespace-blind and runs before root-element check
**File:** `src/FixPortal.FixAtdl/Xml/StrategiesReader.cs` ~line 117  
**Severity:** Low  

**Problem:** `document.Descendants().Any(e => e.Name.LocalName == "RepeatingGroup")` matches any element named `RepeatingGroup` in any XML namespace. A valid FIXatdl document containing a vendor-extension element `<ext:RepeatingGroup>` in a custom namespace would be incorrectly rejected. The check also runs before the O(1) root-element lookup, causing the entire document tree to be traversed before the cheapest possible guard fires.

**Fix:** Qualify with the FIXatdl namespace: `e.Name == AtdlNamespaces.core + "RepeatingGroup"`. Move the check to after the root-element guard.

---

### Pre-existing bugs (not introduced by batch 8 — found during review)

These were present before this branch. They are worth fixing here since the files were already touched (F9, F10) or since they are closely related to the batch 8 work (F11).

---

#### F9 — AtdlValueType.cs: Missing `this` in ThrowHelper.New shifts all args in GetWireValue Invalid branch
**File:** `src/FixPortal.FixAtdl/Model/Types/Support/AtdlValueType.cs` ~line 169  
**Severity:** Low (diagnostic message quality)  
**Origin:** Pre-existing — not introduced by batch 8 (file was touched for nullability only)

**Problem:** The `ThrowHelper.New<InvalidFieldValueException>` call in the `ValidateValue().Invalid` branch of `GetWireValue` is missing `this` as its first argument. The format-string constant is passed as the context object, and the parameter name is passed as the format template. The error message is either the parameter name alone or a `FormatException` if the name contains `{`.

**Fix:** Insert `this` as the first argument, matching the call pattern used everywhere else in the codebase.

---

#### F10 — AtdlReferenceType.cs: Same missing-this bug, mirrored class
**File:** `src/FixPortal.FixAtdl/Model/Types/Support/AtdlReferenceType.cs` ~line 168  
**Severity:** Low (diagnostic message quality)  
**Origin:** Pre-existing — not introduced by batch 8 (file was touched for nullability only)

**Problem:** Identical to F9. The reference-type base class has the same `ThrowHelper.New` call missing `this`.

**Fix:** Same as F9.

---

#### F11 — EnumPair_t.cs: `WireValue = null!` initialiser does not prevent null propagation
**File:** `src/FixPortal.FixAtdl/Model/Types/EnumPair_t.cs` ~line 21  
**Severity:** Low  
**Origin:** Pre-existing — `EnumPair_t.cs` was not changed in batch 8

**Problem:** `public string WireValue { get; set; } = null!;` suppresses the compiler null warning but does not assign a non-null value. A malformed ATDL document that omits the `wireValue` attribute on an `<EnumPair>` element leaves `WireValue` as `null` at runtime. This propagates through `EnumPairCollection.GetWireValueFromEnumId` into BinaryControlBase and ultimately into FIX output as a null tag value.

**Fix:** Either initialise to `string.Empty` and treat empty as absent, or add a null guard in `GetWireValueFromEnumId`.

---

## Summary table

| ID | File | Severity | Origin | Issue |
|----|------|----------|--------|-------|
| F1 | `Boolean_t.cs:40` | High | Batch 8 | FalseWireValue default-value collision undetected by setter |
| F2 | `AtdlReferenceType.cs:149` | High | Batch 8 | `WireValue=""` stored but getter returns null → FIX tag silently omitted |
| F3 | `Edit_t.cs:476` | Medium | Batch 8 | NaN/Infinity comparison incorrect (IEEE 754 violation) |
| F4 | `Edit_t.cs:382` | Medium | Batch 8 | Data_t GT/LT silently false instead of domain error |
| F5 | `DateTimeTypeBase.cs:174` | Medium | Batch 8 | `ToString()` hardcodes `formats[0]` — milliseconds dropped on display path |
| F6 | `Percentage_t.cs:97` | Medium | Batch 8 | Precision applied to fraction not wire value when `MultiplyBy100=false` |
| F7 | `Tenor.cs:161` | Medium | Batch 8 | `IComparable` contract violated + equality/ordering inconsistent for `default(Tenor)` |
| F8 | `StrategiesReader.cs:117` | Low | Batch 8 | `LocalName` match is namespace-blind; guard runs before root-element check |
| F9 | `AtdlValueType.cs:169` | Low | Pre-existing | Missing `this` in `ThrowHelper.New` → garbled error on Invalid validation |
| F10 | `AtdlReferenceType.cs:168` | Low | Pre-existing | Same as F9, reference-type base class |
| F11 | `EnumPair_t.cs:21` | Low | Pre-existing | `null!` initialiser does not prevent null wire value propagation |

---

## Review methodology (for future reference)

This review ran in two passes:

**Pass 1 — Standard (7 angles × 6 candidates):**
Line-by-line diff scan · Removed-behaviour audit · Cross-file caller trace · Reuse · Simplification · Efficiency · Altitude. All candidates sent to one-vote verifiers.

**Pass 2 — Full-bore (12-finder workflow):**
12 parallel agents each deep-reading a specific file slice (all 48 changed files covered), plus dedicated passes for test-code correctness and the WireValue nullability cascade. 62 agents total; 70 raw candidates → 50 after dedup → 38 after verification → top 15 returned.

**Suggested standard format for future review reports:**
- Header: repo, branch, reviewer, method, date, status
- Per-finding: ID, file:line, severity, origin (in-diff vs pre-existing), problem, failure scenario, fix
- Summary table
- Methodology note
