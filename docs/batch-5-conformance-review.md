# Batch 5 — Conformance Review against real broker ATDL

**Date:** 2026-05-31
**Branch:** `reviewer-findings-batch5`
**Reviewer:** structured audit (not the adversarial-review panel — Phase 3 remains deferred)

## What was reviewed

`fixportal-fixatdl` (FIXatdl 1.1 parser + model) audited against **two distinct, live
production broker ATDL documents** extracted from
`D:\Centerprise\work\tomi-database\Tomi\Data\ems.FixAtdlAlgos_Data.sql`:

| Source row | BrokerId | Size | Notes |
|---|---|---|---|
| Id 7 / Id 8 | 82 / 1038 | 1,395 lines | byte-identical; JPM-style (AQUA/STEALTH/SNIPER/VWAP/POC/IS/SWITCH) |
| Id 9 | 431 | 2,090 lines | uses `Clock_t` + `localMktTz="Europe/Berlin"` feeding `UTCTimestamp_t` |

These two documents are being added to the test project as conformance fixtures (see
Finding tracking below).

### Construct surface exercised by the specs

- **Controls:** `CheckBox_t`, `Clock_t`, `DropDownList_t`, `SingleSpinner_t`, `TextField_t`
  (attrs incl. `increment`, `incrementPolicy`, `initValue`, `initValueMode`, `localMktTz`).
- **Parameter types:** `Char_t`, `Float_t`, `Int_t`, `Price_t`, `Qty_t`, `String_t`,
  `UTCTimestamp_t` (attrs incl. `constValue`, `definedByFIX`, `precision`, `minValue`,
  `maxValue`, `mutableOnCxlRpl`, `use`).
- **Edits:** operators `EQ/NE/LT/LE/GE/EX/NX`, logic `AND/OR/XOR`, two-field (`field`+`field2`).
- **Other:** `StateRule@enabled/@value`, `StrategyEdit@errorMessage`, `EnumPair@index`,
  `Strategies@changeStrategyOnCxlRpl/@strategyIdentifierTag`, `StrategyPanel` attrs.

**Bottom line:** parse fidelity is strong — of ~46 attributes exercised, only one is
silently dropped, and most "suspect" attributes are correctly mapped. The real defects are
**semantic**, concentrated in time/timezone handling and presence-based rule evaluation.

---

## Findings (severity-ranked)

> Citations are `file:line` against the source at the time of review. Each "Confirmed"
> finding was verified by reading the cited code.

### CRITICAL

#### C1 — `localMktTz` never applied → wrong instant on the FIX wire
- **Construct:** `Clock_t@localMktTz` feeding `UTCTimestamp_t` (broker-431, fixTag 7113/7114).
- **Evidence:** `<Control xsi:type="lay:Clock_t" … initValue="08:00:00" localMktTz="Europe/Berlin">`
  feeding `<Parameter … xsi:type="UTCTimestamp_t" fixTag="7113">`.
- **Current behavior:** `Clock_t.LocalMktTz` is parsed and stored but explicitly never applied
  (`Model/Controls/Clock_t.cs:35-39,46-50`). The control value flows out as a
  `DateTimeKind.Unspecified` value; `UTCDataTimeTypeBase.GetAdjustedValue` maps `Unspecified`
  via `DateTime.SpecifyKind(…, Utc)` — i.e. **relabels** the local wall-clock as UTC **without
  shifting** (`Model/Types/Support/UTCDataTimeTypeBase.cs:83-88`). So 23:59:59 Berlin is emitted
  as 23:59:59Z — off by the Berlin offset (1–2h, DST-dependent).
- **Correct behavior:** a value expressed in `localMktTz` must be converted from that zone to
  UTC before populating a `UTCTimestamp_t` field. The conversion must originate in `Clock_t`
  (the only place that knows the market zone). NodaTime at that seam:
  `LocalTime`/`LocalDateTime` + `DateTimeZone` (TZDB) → `ZonedDateTime` → `Instant` → UTC.
- **Confidence:** Confirmed (verified `GetAdjustedValue` relabel mechanism directly).

#### C2 — Time-only value parsed with today's date → date-contaminated validation
- **Construct:** `UTCTimestamp_t@maxValue="23:59:59"` (broker-431 `p_EndTime`); same root affects
  `Clock_t@initValue` time-only values.
- **Evidence:** `<Parameter name="p_EndTime" xsi:type="UTCTimestamp_t" fixTag="7114" maxValue="23:59:59" />`.
- **Current behavior:** `FixDateTime.TryParse` matches the `HH:mm:ss` format
  (`Fix/FixDateTimeFormat.cs:23,40-48`) with `AssumeUniversal`; a time-only `TryParseExact`
  fills the **date** component with the parse-time "today" (`Fix/FixDateTime.cs:36`). The stored
  `MaxValue` thus carries an arbitrary run-date, and range comparison
  (`Model/Types/Support/DateTimeTypeBase.cs`) compares a full timestamp against `<today>T23:59:59`
  → non-deterministic across calendar days.
- **Correct behavior:** a time-only FIX value is a time-of-day; the bound/comparison must be on
  the time component (or modelled as a time-of-day), not contaminated by an injected date.
- **Confidence:** Confirmed (parse path + comparison cited).

### HIGH

#### H1 — `EX`/`NX` always wrong for list controls
- **Construct:** operators `EX`/`NX` on list controls (`DropDownList`, `MultiSelectList`,
  `RadioButtonList`, `CheckBoxList`). NX is the most-used operator (66×); `DropDownList` the
  most-used control (115×).
- **Current behavior:** `Edit_t.EvaluateExists` treats a value as absent only when
  `value == null || value as string == string.Empty` (`Model/Elements/Edit_t.cs:315-324`). List
  controls return a never-null `EnumState` from `GetCurrentValue()`
  (`Model/Controls/Support/ListControlBase.cs:112-115`) — "nothing selected" is an all-false
  `EnumState`, not null and not a string. So **EX ≡ true, NX ≡ false** regardless of selection.
- **Correct behavior:** EX/NX must reflect real presence; an unselected list control must read
  as not-exists. Needs an "EnumState has no selection" predicate.
- **Confidence:** Confirmed (verified call site `Edit_t.cs:292-299` feeds the raw control value).

#### H2 — Inequality vs a missing FIX field returns a definite wrong boolean
- **Construct:** `LT/LE/GE/GT` with `field2="FIX_*"` against an absent inbound FIX field
  (broker-82 `field2="FIX_Price"`, `field2="FIX_OrderQty"`).
- **Current behavior:** a missing RHS field resolves to `null`; the type-guard is skipped and it
  falls through to `lhs.CompareTo(null)` (`Model/Elements/Edit_t.cs:350,355`), which returns +1
  for `decimal`/`DateTime` (value > null) → e.g. `GE`→true, `LT`→false. A definite boolean is
  produced from missing data. Masked in these specs by OR-wrapped NX guards, but wrong standalone.
- **Correct behavior:** a missing comparison operand is indeterminate; the RHS-null case should be
  handled symmetrically to the existing LHS-null short-circuit (`Edit_t.cs:341-343`).
- **Confidence:** Confirmed.

#### H3 — `EnumPair@index` silently dropped
- **Construct:** `EnumPair@index` (broker-82, 128×): `<EnumPair enumID="o1" index="0" wireValue="0" />`.
- **Current behavior:** not declared in `Xml/SchemaDefinitions.cs` (EnumPairs maps only
  `enumID`/`wireValue`); `Model/Elements/EnumPair_t.cs` has no `Index` property; the reflective
  factory ignores undeclared attributes → silent drop.
- **Correct behavior — TO VERIFY:** standard FIXatdl 1.1 `EnumPair` is `enumID`+`wireValue`; `index`
  may be a **vendor extension**. Action: confirm against the FIXatdl 1.1 XSD. If standard (or if
  ordering is semantically relied upon), add an `int? Index`. If a vendor extension, make the
  silent-ignore a *deliberate, documented* decision.
- **Confidence:** Confirmed (drop); spec-correctness pending XSD check.

#### H4 — `definedByFIX` parsed but never consumed
- **Construct:** `Parameter@definedByFIX="true"` (broker-82, e.g. `OrdType` tag 40, `OrderQty` tag 38).
- **Current behavior:** mapped to `Parameter_t.DefinedByFix` (`SchemaDefinitions.cs:89`) but no
  reader anywhere — gates nothing.
- **Correct behavior:** per FIXatdl 1.1, marks a parameter as a redefinition of a standard FIX tag
  (wire field already carries FIX-defined semantics/enums). Decide what it should gate (at minimum
  do not silently imply it has no meaning). *(Casing is correct: `definedByFIX`→`DefinedByFix`.)*
- **Confidence:** Confirmed (no consumer found).

### MEDIUM

#### M1 — UTC types bypass `WireParseStyles`; latent host-offset bug
- `UTCDateTimeTypeBase.ConvertFromWireValueFormat` is hardcoded to `AssumeUniversal` **without**
  `AdjustToUniversal` (`UTCDataTimeTypeBase.cs:40-52`), so the virtual `WireParseStyles`
  (`DateTimeTypeBase.cs`) is dead for the UTC family, and a `Local`-kind value reaching
  `GetAdjustedValue` is shifted by the host offset. `TZTimestamp_t` does it correctly
  (`TZTimestamp_t.cs:47-48`). Benign for pure wire round-trips today, but a latent host-dependent
  defect and a misleading abstraction. *Closely related to C1/C2 — fix together.*
- **Confidence:** Confirmed (dead override); Likely (observable mis-conversion needs a Local value).

#### M2 — Edit with both `value` and `field2`: `value` silently wins
- `GetRhsValue` returns `Value` whenever non-null, falling back to `Field2` only when `Value` is null
  (`Edit_t.cs:406-424`); no parse-time guard forbids both. Latent (real specs never set both) but a
  silent correctness trap. Action: reject "both set" at parse/resolve, or document the precedence.
- **Confidence:** Likely (behavior confirmed; impact latent).

#### M3 — Unset checkbox vs `EQ "false"` is init-order dependent
- An uninitialized binary control is `null`; `AreEqual(null, "false")` returns false
  (`Edit_t.cs:369-373`), so a StateRule keyed on `EQ "false"` won't fire until the checkbox is
  explicitly set (broker-431 `EnableStartTime`/`EnableEndTime` pattern). Outcome depends on whether
  binary controls are initialized to a concrete `false` before evaluation.
- **Confidence:** Suspected (hinges on control init policy).

#### M4 — Float/Price/Qty precision rounding-mode assumption
- `Math.Round(value, precision, MidpointRounding.AwayFromZero)` (`Float_t.cs:166-169`); FIXatdl does
  not mandate a rounding mode, and trailing zeros are not padded (wire-legal). Low practical impact;
  confirm `AwayFromZero` is the intended convention.
- **Confidence:** Confirmed (precision applied); Suspected (mode mismatch, low).

### NOTES — confirmed correct / by design (no fix)

- **N1** — XOR computes the correct result for any operand count; it simply doesn't short-circuit
  (`EditEvaluatingCollection.cs:122-129`). Perf only.
- **N2** — StateRule `enabled`/`visible`/`value` are *evaluated* but never *applied* to controls
  inside this library (`StateRuleCollection.cs:44-50`); the consuming UI applies them. By design for
  a model library — worth documenting the boundary explicitly.
- Verified conformant: `constValue` semantics, inclusive `minValue`/`maxValue` bounds,
  Char/String/Int/Float round-trips, operator-name mappings, and full element coverage
  (StrategyPanel/StrategyLayout/Region(s)/SecurityType(s)).

---

## Disposition (batch 5 scope = everything actionable)

| ID | Severity | Plan |
|---|---|---|
| C1 | Critical | Apply `localMktTz` in `Clock_t` via NodaTime (zone→UTC); design spec to follow |
| C2 | Critical | Model time-only FIX values as time-of-day; remove injected-date contamination |
| H1 | High | Add EnumState "no selection" presence test; route EX/NX through it |
| H2 | High | Treat null RHS as indeterminate (symmetric with LHS-null short-circuit) |
| H3 | High | Verify against FIXatdl 1.1 XSD; add `Index` if standard, else document deliberate ignore |
| H4 | High | Decide + implement what `definedByFIX` gates (or document why it's inert) |
| M1 | Medium | Fix UTC parse styles (`AdjustToUniversal`) / retire dead `WireParseStyles`; with C1/C2 |
| M2 | Medium | Guard/validate "both `value` and `field2`"; document precedence |
| M3 | Medium | Ensure binary controls initialize to concrete `false` before evaluation |
| M4 | Medium | Confirm rounding-mode convention; document |

Fixes, fixtures (broker-82 + broker-431), and conformance tests land on this branch; merged to
`main` via rebase-merge PR.

---

## Resolution log

All actionable findings are now resolved across three phases (each merged to `main` via its own
rebase-merge PR):

| ID | Phase | Resolution |
|---|---|---|
| C1 | A | `Clock_t` applies `localMktTz` via NodaTime (zone→UTC); emits UTC at the wire boundary. |
| M1 | A | UTC family routes through `WireParseStyles` + `AdjustToUniversal`. |
| C2 (clock half) | A | `InitValueClock` holds `LocalTime`/`LocalDateTime` — no injected date. |
| H1 | B | `EnumState.HasSelection`; `EvaluateExists` treats an unselected list control as absent. |
| H2 | B | `EvaluateInequalityComparison` null-RHS → `false` (symmetric with null-LHS). |
| M2 | B | `Edit_t<T>.Resolve` rejects both `value` + `field2` (mutually exclusive RHS forms). |
| M3 | B | `BinaryControlBase._value` defaults to concrete `false`. |
| H3 | C | `EnumPair@index` captured as `int? Index` (optional; does not affect wire output). |
| H4 | C | `Parameter_t.DefinedByFix` is a documented, deliberately-inert contract (no validation gate). |
| M4 | C | `Float_t.Round` documents the `MidpointRounding.AwayFromZero` convention (and is now `static`). |
| C2 (bound half) | C | Time-only `maxValue`/`minValue` on UTC/TZ timestamp types compare on the time-of-day component only — no injected-date contamination. |

**Phase-C follow-ups also delivered:** `FixDateTime.TryParse` now normalises to canonical `Kind=Utc`
(`AdjustToUniversal`), with `Clock_t.LoadDefaultFromFixValue` pinned by test; `Clock_t.ToString`
preserves sub-second precision. The SonarAnalyzer backlog (24 src findings + the test-project
CA1859/CS8601/Sonar warnings surfaced once src compiled clean) was cleared, and
`TreatWarningsAsErrors` is now enforced solution-wide (clean `--no-incremental` build is 0/0).

**Still deferred (not batch 5):** N1/N2 remain by-design (see above); the broker-82/broker-431
conformance **fixtures** are deferred to Phase D (real client data — must be obfuscated before
committing); the adversarial-review panel (Phase 3) remains deferred.
