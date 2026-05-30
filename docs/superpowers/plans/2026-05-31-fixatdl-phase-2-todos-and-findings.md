# FixPortal.FixAtdl Phase 2 — Clear TODOs + Batch-3 Deferred Findings Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Resolve every outstanding in-code `// TODO` in `src/` and every deferred batch-3 adversarial-review finding — each either *fixed* with a guarding test, or *explicitly closed* with written rationale (or converted to a tracked GitHub issue), so the tree is finding-clean and TODO-clean going into the Phase 3 review.

**Architecture:** Behaviour-changing work, gated on the Phase 1 test net (557 tests, 69% line coverage). Two findings are genuine fixes (TDD: failing test → minimal change → green); the remaining items are vestigial TODOs or Low/contested findings whose correct disposition — after reading the source — is *close-with-rationale*, recorded in a durable disposition doc. We do **not** manufacture changes: batch 3 already remediated the must-fixes (the `ConvertToComparableType` exception-wrap M4, the `ConvertToBool` culture/null guards L3, the `Tenor.Parse` OverflowException catch #8, the `ModelUtils` reflection hardening, the dead `NonNegativeIntegerTypeBase` null-guard) — verified present in the current source while writing this plan. Phase 2's honest output is **2 fixes + 1 tracked issue + 7 reasoned closures**, all auditable in one disposition doc.

**Tech Stack:** .NET 10; xUnit v3 + AwesomeAssertions (`.Should()`) + NSubstitute (test project global usings: `AwesomeAssertions`, `NSubstitute`, `Xunit`); `dotnet build` 0-warning bar; `dotnet test`; CI coverage floor (65%) in `build-and-test.yml`.

**Branch:** Normal feature branch `phase-2-todos-and-findings` in the **primary checkout** (`D:\FixPortal\fixportal-fixatdl`), branched from `origin/main`. This is *not* a reviewer-findings pass, so the review-worktree workflow does **not** apply (that is reserved for Phase 3's adversarial review on `reviewer-findings-batch5`). PR-only to `main`, rebase-merge.

---

## Disposition summary

| # | Item | Source | Severity | Disposition | Task |
|---|------|--------|----------|-------------|------|
| O-G2 | `ConvertToComparableType` null-`value` path: numerics silently coerce `null`→`0`, enums/MonthYear/Tenor NRE | report-10 | Low/Med | **FIX** — fail-fast `InvalidFieldValueException`; guarding test | 1 |
| G-D | `ThrowHelper.CreateException<T>` hard-codes `paramName="Value"` for `ArgumentNullException`/`ArgumentOutOfRangeException` | report-11 | Medium | **FIX** — thread real param name (optional `paramName`, default preserves behaviour); guarding test | 2 |
| F7 | `TagNum_t` accepts leading zeros (`"0044"`→44) | report-02 | Low (contested 1:1:1) | **CLOSE w/ rationale** — enforcement-tightening, not a correctness defect | 3 |
| #7 | `Tenor.Parse` accepts non-positive offsets (`D0`, `M-3`) | report-01 | Low (contested 2:1 needs-evidence) | **CLOSE w/ rationale** — ambiguous spec (`D0` = same-day in some impls); range belongs to business layer | 3 |
| G-G | `IParentable<T>.Parent` non-nullable → `null!` at roots | report-11 | Low | **CLOSE w/ rationale** — public-API nullability ripple to every consumer outweighs removing an internal sentinel for a never-in-a-parsed-model condition | 4 |
| TODO | `EditEvaluator<T>` "Implement IDisposable" | `EditEvaluator.cs:17` | — | **CLOSE** — no disposable members/events/unmanaged resources; Notification assembly removed (Task A8) | 5 |
| TODO | `StateRule_t` "Implement IDisposable" | `StateRule_t.cs:15` | — | **CLOSE** — inherits `EditEvaluator`; same rationale | 5 |
| TODO | `EditEvaluatingCollection<T>` "Unbind needed?" | `EditEvaluatingCollection.cs:138` | — | **CLOSE** — `IBindable<T>` is dead (defined, never implemented/called); model rebuilt fresh per parse; remove dead interface | 5 |
| TODO | `ModelUtils` "Move this somewhere better" | `ModelUtils.cs:91` | — | **CLOSE** — `ModelUtils` owns the `_types` cache `GetTypeFromName` reads; correct home | 5 |
| TODO | `Clock_t` "Implement LocalMktTz as a type" | `Clock_t.cs:35` | — | **DEFER → GitHub issue** — feature (timezone resolution), not a bug; out of proportion to a TODO-clearance phase | 6 |

**Out of scope (recorded, not actioned here):** the batch-3 *coverage* gaps the roadmap floated (`FixFieldValueProvider` percentage/enum-pair branches, `ReadOnlyControlCollection` integration paths) are test-coverage items, not behaviour findings — they belong to Phase 1's remit or the Phase 3 review, not a TODO/finding-remediation pass. The roadmap's `NonNegativeIntegerTypeBase.ValidateValue` "dead path" candidate is **already resolved** — the dead null-guard was removed in batch 3 (see the comment at `NonNegativeIntegerTypeBase.cs:50`); `ValidateValue` now only carries the `isRequired` check. The unanimous `Tenor.Parse` OverflowException finding (#8) is **already fixed** (`Tenor.cs:137`). Task 7 records all of this.

---

## File Structure

**Source changes (2 fixes):**
- Modify: `src/FixPortal.FixAtdl/Validation/EditValueConverter.cs` — null-`value` guard (O-G2).
- Modify: `src/FixPortal.FixAtdl/Diagnostics/ThrowHelper.cs` — thread `paramName` (G-D).

**Source changes (TODO closures — comment/dead-code only, no behaviour change):**
- Modify: `src/FixPortal.FixAtdl/Validation/EditEvaluator.cs` — replace TODO with rationale comment.
- Modify: `src/FixPortal.FixAtdl/Model/Elements/StateRule_t.cs` — replace TODO with rationale comment.
- Modify: `src/FixPortal.FixAtdl/Model/Collections/EditEvaluatingCollection.cs` — replace TODO with rationale comment.
- Delete: `src/FixPortal.FixAtdl/Utility/IBindable.cs` — dead internal interface (the binding concept "Unbind needed?" referred to).
- Modify: `src/FixPortal.FixAtdl/Utility/ModelUtils.cs` — remove TODO.
- Modify: `src/FixPortal.FixAtdl/Model/Controls/Clock_t.cs` — replace TODO with GitHub-issue reference.

**Tests:**
- Modify: `tests/FixPortal.FixAtdl.Tests/Validation/EditValueConverterTests.cs` — null-`value` guarding tests (O-G2).
- Create: `tests/FixPortal.FixAtdl.Tests/Diagnostics/ThrowHelperTests.cs` — paramName-threading test (G-D).
- Modify: `tests/FixPortal.FixAtdl.Tests/Model/Types/Support/TenorTests.cs` — characterization test pinning the *accepted* `D0`/`M-3` behaviour we are deliberately keeping (#7).
- Modify: `tests/FixPortal.FixAtdl.Tests/Model/Types/TypeCoverageGapTests.cs` — characterization test pinning the *accepted* `TagNum_t` leading-zero behaviour (F7).

**Docs:**
- Create: `docs/batch-3-findings-disposition.md` — durable record of every fix/close/defer with rationale.

---

## Task 1: Fix O-G2 — `ConvertToComparableType` null-`value` fail-fast

**Background for the implementer:** `EditValueConverter.ConvertToComparableType(object typeInstanceToMatch, string value)` (in `src/FixPortal.FixAtdl/Validation/EditValueConverter.cs`) dispatches on the prototype's runtime type. Batch 3 already added the `catch (FormatException or OverflowException) → InvalidFieldValueException` wrap (lines 64-69) and the `ConvertToBool` null guard. The remaining gap (O-G2): when `value` itself is `null`, behaviour is **type-dependent and inconsistent** — `Convert.ToDecimal(null)`/`Convert.ToInt32(null)`/`Convert.ToUInt32(null)` silently return `0` (a missing Edit right-hand-side is masked as the number zero, which can make a comparison spuriously pass), while `MonthYear.Parse(null)`/`Tenor.Parse(null)`/`value.ParseAsEnum<…>()` throw `NullReferenceException`. The fix makes a null operand fail fast and consistently with a domain `InvalidFieldValueException`, the same way `ConvertToBool` already rejects null. `ErrorMessages.IllegalUseOfNullError` already exists (used at `EditValueConverter.cs:76`).

**Files:**
- Test: `tests/FixPortal.FixAtdl.Tests/Validation/EditValueConverterTests.cs`
- Modify: `src/FixPortal.FixAtdl/Validation/EditValueConverter.cs:34-41`

- [ ] **Step 1: Write the failing tests**

Add to `EditValueConverterTests.cs`, in the `// ── Null prototype ──` region (just below `Null_prototype_returns_value_unchanged`):

```csharp
    // ── Null value (O-G2): a missing operand must fail fast, not coerce to 0 ──

    [Theory]
    [InlineData(typeof(decimal))]   // was: Convert.ToDecimal(null) => 0m  (silent)
    [InlineData(typeof(int))]       // was: Convert.ToInt32(null)   => 0   (silent)
    [InlineData(typeof(uint))]      // was: Convert.ToUInt32(null)  => 0u  (silent)
    public void Null_value_throws_InvalidFieldValueException_for_numeric_types(Type prototypeType)
    {
        object prototype = Activator.CreateInstance(prototypeType)!;
        var act = () => EditValueConverter.ConvertToComparableType(prototype, null!);
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Null_value_throws_InvalidFieldValueException_not_NullReference_for_month_year()
    {
        // Previously NRE inside MonthYear.Parse(null).
        var act = () => EditValueConverter.ConvertToComparableType(default(MonthYear), null!);
        act.Should().Throw<InvalidFieldValueException>();
    }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test D:\FixPortal\fixportal-fixatdl\FixPortal.FixAtdl.sln --filter "FullyQualifiedName~EditValueConverterTests"`
Expected: the three numeric `[InlineData]` cases FAIL (they return `0`/`0u` instead of throwing); the MonthYear case FAILS with `NullReferenceException` rather than `InvalidFieldValueException`.

- [ ] **Step 3: Add the null-`value` guard**

In `EditValueConverter.cs`, change the guard block at the top of `ConvertToComparableType` (currently lines 36-40):

```csharp
        // If we don't have a valid type to convert to, then best leave the value alone.
        if (typeInstanceToMatch == null)
        {
            return value;
        }
```

to:

```csharp
        // If we don't have a valid type to convert to, then best leave the value alone.
        if (typeInstanceToMatch == null)
        {
            return value;
        }

        // A null operand is a missing Edit value, not a zero. The numeric Convert.To* paths would
        // silently coerce null to 0 (masking a missing right-hand side and making comparisons pass
        // spuriously) while the enum/MonthYear/Tenor paths would NRE. Reject it consistently with a
        // domain exception, matching ConvertToBool's null handling (O-G2).
        if (value == null)
        {
            throw ThrowHelper.New<InvalidFieldValueException>(ExceptionContext, ErrorMessages.IllegalUseOfNullError);
        }
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test D:\FixPortal\fixportal-fixatdl\FixPortal.FixAtdl.sln --filter "FullyQualifiedName~EditValueConverterTests"`
Expected: all `EditValueConverterTests` PASS (including the pre-existing `Null_prototype_returns_value_unchanged`, which is unaffected — the prototype-null branch returns before the new value-null guard).

- [ ] **Step 5: Build clean and commit**

Run: `dotnet build D:\FixPortal\fixportal-fixatdl\FixPortal.FixAtdl.sln -c Release`
Expected: 0 errors, 0 new warnings.

```
git add src/FixPortal.FixAtdl/Validation/EditValueConverter.cs tests/FixPortal.FixAtdl.Tests/Validation/EditValueConverterTests.cs
```
```
git commit -m "fix(validation): reject null Edit operand in ConvertToComparableType (O-G2)"
```
(Footer: `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>`.)

---

## Task 2: Fix G-D — thread real `paramName` through `ThrowHelper.CreateException<T>`

**Background for the implementer:** `ThrowHelper.CreateException<T>(object? source, string message, ExceptionInfo? info)` in `src/FixPortal.FixAtdl/Diagnostics/ThrowHelper.cs` (lines 232-263) special-cases `ArgumentOutOfRangeException`/`ArgumentNullException` (whose `(string, string)` ctor is `(paramName, message)`) and **hard-codes `"Value"`** as the param name (line 245: `classConstructor.Invoke(["Value", message])`). Every domain `ArgumentException`-family throw routed through `ThrowHelper` therefore reports `ParamName = "Value"` regardless of the real argument (e.g. `ParseAsEnum`). The fix threads an optional `paramName` from the public `New<T>` overloads down to `CreateException`, defaulting to `"Value"` so all current callers are behaviour-identical, then has the relevant call site(s) pass the real name.

**Files:**
- Modify: `src/FixPortal.FixAtdl/Diagnostics/ThrowHelper.cs`
- Test: `tests/FixPortal.FixAtdl.Tests/Diagnostics/ThrowHelperTests.cs` (create)

- [ ] **Step 1: Read the public `New<T>` surface**

Read `src/FixPortal.FixAtdl/Diagnostics/ThrowHelper.cs` lines 1-150 to see the eight `New<T>` overloads (they call `CreateException<T>` at lines 34, 49, 65, 81, 96, 112, 128, 145). Identify the overload shape used to raise `ArgumentNullException`/`ArgumentOutOfRangeException` (the simple `New<T>(object? source, string message)` / `New<T>(object? source, string format, params object[] args)` forms — confirm by grepping call sites in Step 4).

- [ ] **Step 2: Write the failing test**

Create `tests/FixPortal.FixAtdl.Tests/Diagnostics/ThrowHelperTests.cs`:

```csharp
using FixPortal.FixAtdl.Diagnostics;

namespace FixPortal.FixAtdl.Tests.Diagnostics;

/// <summary>
/// Tests for <see cref="ThrowHelper"/> exception construction, focused on the
/// ArgumentException-family ParamName threading (G-D).
/// </summary>
public class ThrowHelperTests
{
    [Fact]
    public void New_argument_exception_threads_supplied_param_name()
    {
        // The (string, string) ctor of ArgumentException-family types is (paramName, message);
        // ThrowHelper must surface the real parameter name, not a hard-coded "Value".
        ArgumentOutOfRangeException ex = ThrowHelper.New<ArgumentOutOfRangeException>(
            source: null, message: "out of range", paramName: "tenorOffset");

        ex.ParamName.Should().Be("tenorOffset");
    }

    [Fact]
    public void New_argument_exception_defaults_param_name_when_not_supplied()
    {
        // Back-compat: existing callers that pass no name keep the historical "Value".
        ArgumentOutOfRangeException ex = ThrowHelper.New<ArgumentOutOfRangeException>(
            source: null, message: "out of range");

        ex.ParamName.Should().Be("Value");
    }
}
```

> If the existing `New<T>(object?, string)` overload signature differs from the call shown (e.g. the first positional is the source object), adjust the test call to match the real signature discovered in Step 1 — the *assertions* (`ParamName` is the supplied name, else `"Value"`) are the contract under test.

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test D:\FixPortal\fixportal-fixatdl\FixPortal.FixAtdl.sln --filter "FullyQualifiedName~ThrowHelperTests"`
Expected: FAIL — either it doesn't compile (no `paramName` parameter yet) or, once a stub overload exists, `New_argument_exception_threads_supplied_param_name` fails because `ParamName` is `"Value"`.

- [ ] **Step 4: Thread `paramName` through `CreateException` and one `New<T>` overload**

In `CreateException<T>(object? source, string message, ExceptionInfo? info)` add an optional trailing parameter and use it in the ArgumentException branch. Change the signature (line 232) and the branch (line 245):

```csharp
    private static T CreateException<T>(object? source, string message, ExceptionInfo? info, string paramName = "Value") where T : Exception
    {
        Type classType = typeof(T);

        switch (classType.Name)
        {
            case "ArgumentOutOfRangeException":
            case "ArgumentNullException":
                {
                    ConstructorInfo classConstructor = classType.GetConstructor([typeof(string), typeof(string)])
                        ?? throw new InternalErrorException($"Exception type '{classType.FullName}' has no (string, string) constructor required by ThrowHelper. Message: {message}");
                    T exception = (T)classConstructor.Invoke([paramName, message]);
                    exception.Source = source?.ToString();
                    info?.PopulateExceptionData(exception.Data);

                    return exception;
                }
            // ... default branch unchanged ...
```

Add a public overload that carries the name (place it next to the existing `New<T>(object? source, string message)` overload around line 30-40):

```csharp
    /// <summary>
    /// Creates an exception of type <typeparamref name="T"/> with an explicit parameter name, for the
    /// ArgumentException family whose (string, string) constructor is (paramName, message).
    /// </summary>
    public static T New<T>(object? source, string message, string paramName) where T : Exception
    {
        return CreateException<T>(source, message, null, paramName);
    }
```

> Keep the change minimal: only `CreateException<T>(object?, string, ExceptionInfo?)` gains the optional `paramName` (the inner-exception and format overloads are not the ones raising bare `ArgumentNullException`/`ArgumentOutOfRangeException`; confirm with the Step-1 grep). Do not alter the `default:` branch behaviour.

- [ ] **Step 5: Point real Argument-family call sites at the new overload**

Run: `Grep` for `New<ArgumentNullException>` and `New<ArgumentOutOfRangeException>` across `src/`. For each call site, pass the real argument name as the third parameter where it is statically known (e.g. a `nameof(...)`). If a grep finds **no** such call sites (the family is only reached reflectively via `Rethrow`/`ParseAsEnum`), record that in the commit message — the threading capability still removes the hard-coded `"Value"` misinformation for any future/ reflective caller, and the back-compat default keeps existing behaviour.

- [ ] **Step 6: Run the test to verify it passes; build clean**

Run: `dotnet test D:\FixPortal\fixportal-fixatdl\FixPortal.FixAtdl.sln --filter "FullyQualifiedName~ThrowHelperTests"`
Expected: both tests PASS.
Run: `dotnet build D:\FixPortal\fixportal-fixatdl\FixPortal.FixAtdl.sln -c Release`
Expected: 0 errors, 0 new warnings.

- [ ] **Step 7: Commit**

```
git add src/FixPortal.FixAtdl/Diagnostics/ThrowHelper.cs tests/FixPortal.FixAtdl.Tests/Diagnostics/ThrowHelperTests.cs
```
```
git commit -m "fix(diagnostics): thread real ParamName through ThrowHelper Argument-exception path (G-D)"
```

---

## Task 3: Close F7 (TagNum leading-zeros) and #7 (Tenor non-positive) with rationale + characterization tests

**Background for the implementer:** Both are Low, contested findings. The decision (recorded in `docs/batch-3-findings-disposition.md`, Task 7) is to **keep current behaviour** and pin it with characterization tests so the decision is explicit and regression-guarded — not silently "maybe a bug, maybe not".

- **F7** (`report-02`): `TagNum_t : NonZeroPositiveIntegerTypeBase` parses `"0044"` to `44u` via `Convert.ToUInt32`. The FIX contract says tag numbers carry no leading zeros, but rejecting them is *enforcement-tightening*, not a correctness defect: the parsed numeric value is correct, no conforming FIXatdl emits leading-zero tags, and reviewer S rated it needs-evidence (parser-rejection vs wire-emit rule). We keep the lenient parse.
- **#7** (`report-01`): `Tenor.Parse` accepts `D0` and `M-3` (any integer offset once the unit letter is valid). The panel split 2:1 needs-evidence — `D0` is "same day" in some implementations, and offset-range enforcement is a business-layer concern, not a parser invariant. We keep the lenient parse. (Note: the *unanimous* sibling finding #8, uncaught `OverflowException`, is already fixed at `Tenor.cs:137` — `catch (… or OverflowException)`.)

**Files:**
- Modify: `tests/FixPortal.FixAtdl.Tests/Model/Types/Support/TenorTests.cs`
- Modify: `tests/FixPortal.FixAtdl.Tests/Model/Types/TypeCoverageGapTests.cs`

- [ ] **Step 1: Add the Tenor characterization test (#7)**

Add to `TenorTests.cs` (match the file's existing `using`s / namespace `FixPortal.FixAtdl.Tests.Model.Types.Support`; `Tenor` is in `FixPortal.FixAtdl.Model.Types.Support`):

```csharp
    // Characterization (batch-3 finding #7, deliberately CLOSED — see docs/batch-3-findings-disposition.md):
    // Tenor.Parse intentionally accepts non-positive offsets. D0 means "same day" in several FIX
    // implementations and numeric-range enforcement is a business-layer concern, not a parser invariant.
    // This test pins that decision so a future "tighten the parser" change is a conscious one.
    [Theory]
    [InlineData("D0")]
    [InlineData("M-3")]
    public void Parse_accepts_non_positive_offsets_by_design(string wire)
    {
        var act = () => Tenor.Parse(wire);
        act.Should().NotThrow();
    }
```

- [ ] **Step 2: Add the TagNum leading-zero characterization test (F7)**

Add to `TypeCoverageGapTests.cs` (match its existing `using`s / namespace; `TagNum_t` is in `FixPortal.FixAtdl.Model.Types`). Drive it through the wire-value round-trip the way the other type tests do — set `WireValue`, read `GetCurrentValue()`:

```csharp
    // Characterization (batch-3 finding F7, deliberately CLOSED — see docs/batch-3-findings-disposition.md):
    // TagNum_t parses leading-zero wire values to their numeric value (no leading-zero rejection).
    // Rejecting them is enforcement-tightening, not a correctness fix; the parsed value is correct.
    [Fact]
    public void TagNum_t_parses_leading_zero_wire_value_to_numeric_value()
    {
        var parameter = new Parameter_t<TagNum_t>("p") { WireValue = "0044" };
        parameter.GetCurrentValue().Should().Be(44u);
    }
```

> If `Parameter_t<TagNum_t>` is not the established construction shape in `TypeCoverageGapTests.cs`, follow that file's existing pattern for instantiating a typed parameter and setting its wire value (the Phase 1 tests in this file already exercise several `_t` types this way). The assertion — `"0044"` yields `44u` without throwing — is the contract being pinned.

- [ ] **Step 3: Run the tests to verify they pass (they pin current behaviour, so green immediately)**

Run: `dotnet test D:\FixPortal\fixportal-fixatdl\FixPortal.FixAtdl.sln --filter "FullyQualifiedName~TenorTests|FullyQualifiedName~TypeCoverageGapTests"`
Expected: PASS. If either FAILS, current behaviour differs from the finding's description — **stop and report**; the disposition (close) would need revisiting.

- [ ] **Step 4: Commit**

```
git add tests/FixPortal.FixAtdl.Tests/Model/Types/Support/TenorTests.cs tests/FixPortal.FixAtdl.Tests/Model/Types/TypeCoverageGapTests.cs
```
```
git commit -m "test(types): pin closed batch-3 findings F7 (TagNum leading zeros) and #7 (Tenor non-positive)"
```

---

## Task 4: Close G-G — `IParentable<T>.Parent` nullability, with rationale

**Background for the implementer:** `IParentable<T>.Parent` (`src/FixPortal.FixAtdl/Utility/IParentable.cs`) is declared `T Parent { get; set; }` (non-nullable). Root objects with no parent (`Strategy_t`'s `Strategies_t` parent, a top-level `StrategyPanel_t`, `StateRule_t._owner` before parenting) back it with `= null!`. The finding (Low, single-reviewer gap) is that the non-nullable declaration forces those `null!` sentinels.

**Decision: CLOSE with rationale (no code change).** Making `Parent` nullable (`T? Parent`) is a *public* API change rippling to all five implementers (`Control_t`, `StateRule_t`, `ReadOnlyControlCollection`, `StrategyPanel_t`, `Strategy_t`) and every consumer that reads `.Parent` — pushing null-handling onto callers for a condition that **cannot occur in a fully-parsed model**: `ControlCollection.InsertItem`/`StateRuleCollection.InsertItem` assign the parent at attach time, before any read. The `null!` is a localized, well-understood "not yet attached" sentinel. For a Low finding, on the eve of the Phase 4 public-API freeze, the cost (nullable surface for every consumer) exceeds the benefit (deleting an internal sentinel). We keep the non-nullable contract and record the reasoning.

**This task is documentation-only** — its substance lands in Task 7's disposition doc. No source or test change. (If, at execution time, the user has instead asked to *fix* G-G, the mechanical alternative is: change the interface to `T? Parent { get; set; }`, change all five explicit implementations to `T?`, drop the `= null!` initialisers, and resolve the compiler-flagged read sites with `?.`/null-checks — but that is **not** the disposition this plan adopts.)

- [ ] **Step 1: No action here.** Confirm there is nothing to build/test for this task; its rationale is written in Task 7. Mark complete once Task 7's doc contains the G-G entry.

---

## Task 5: Close the four vestigial lifecycle/relocation TODOs

**Background for the implementer:** Four `// TODO`s are vestigial from the pre-fork atdl4net design (which had a Notification assembly, removed in Task A8 — see `src/FixPortal.FixAtdl/Xml/StrategyLoadedEventArgs.cs:15`). A repo-wide grep confirms **no `+=` / event subscription anywhere in the edit/control/state-rule object graph** — the only events live in `StrategiesReader` and `StrategyPanel_t` (the latter already implements `IDisposable` correctly). So there is nothing to dispose and nothing to unbind. Each TODO is replaced by a one-line rationale comment (or deleted), and the dead `IBindable<T>` interface — the literal subject of "Unbind needed?" — is removed.

**Files:**
- Modify: `src/FixPortal.FixAtdl/Validation/EditEvaluator.cs:17`
- Modify: `src/FixPortal.FixAtdl/Model/Elements/StateRule_t.cs:15`
- Modify: `src/FixPortal.FixAtdl/Model/Collections/EditEvaluatingCollection.cs:138`
- Delete: `src/FixPortal.FixAtdl/Utility/IBindable.cs`
- Modify: `src/FixPortal.FixAtdl/Utility/ModelUtils.cs:91`

- [ ] **Step 1: `EditEvaluator` — replace the TODO**

In `EditEvaluator.cs`, replace line 17:

```csharp
// TODO: Implement IDisposable
```

with:

```csharp
// IDisposable is not needed: this type holds only Edit_t/EditRef_t references (no unmanaged resources,
// no event subscriptions). The disposal contract existed for the removed Notification assembly (Task A8).
```

- [ ] **Step 2: `StateRule_t` — replace the TODO**

In `StateRule_t.cs`, replace line 15:

```csharp
// TODO: Implement IDisposable
```

with:

```csharp
// IDisposable is not needed: StateRule_t inherits only EditEvaluator's edit references and subscribes to
// no events (see EditEvaluator). Vestige of the removed Notification assembly (Task A8).
```

- [ ] **Step 3: `EditEvaluatingCollection` — replace the TODO, remove the dead binding interface**

In `EditEvaluatingCollection.cs`, replace line 138:

```csharp
    // TODO: Unbind needed?
```

with:

```csharp
    // No unbind: Resolve only forwards to each child's Resolve (idempotent), establishing no binding to
    // tear down. The model is rebuilt fresh per parse, and the IBindable<T> mechanism this question
    // referred to was unused and has been removed.
```

Delete the dead interface file: `src/FixPortal.FixAtdl/Utility/IBindable.cs` (internal interface, defined but never implemented or called — verified by grep: only its own declaration matches `IBindable`).

```
git rm src/FixPortal.FixAtdl/Utility/IBindable.cs
```

- [ ] **Step 4: `ModelUtils` — remove the TODO**

In `ModelUtils.cs`, replace line 91:

```csharp
    // TODO: Move this somewhere better.
```

with:

```csharp
    // GetTypeFromName lives here by design: ModelUtils owns the _types cache it reads.
```

- [ ] **Step 5: Build clean and run the full suite (no behaviour changed, but prove it)**

Run: `dotnet build D:\FixPortal\fixportal-fixatdl\FixPortal.FixAtdl.sln -c Release`
Expected: 0 errors, 0 new warnings (deleting `IBindable.cs` must not orphan any reference — the grep proved none exist).
Run: `dotnet test D:\FixPortal\fixportal-fixatdl\FixPortal.FixAtdl.sln`
Expected: all tests PASS (unchanged count from main + the tests added in Tasks 1-3).

- [ ] **Step 6: Commit**

```
git add src/FixPortal.FixAtdl/Validation/EditEvaluator.cs src/FixPortal.FixAtdl/Model/Elements/StateRule_t.cs src/FixPortal.FixAtdl/Model/Collections/EditEvaluatingCollection.cs src/FixPortal.FixAtdl/Utility/ModelUtils.cs
```
```
git commit -m "chore(model): resolve vestigial IDisposable/Unbind/relocation TODOs; drop dead IBindable"
```

---

## Task 6: Defer Clock_t `LocalMktTz` to a tracked GitHub issue

**Background for the implementer:** `Clock_t.LocalMktTz` is a `string?` today; the TODO ("Implement LocalMktTz as a type") asks for strongly-typed timezone resolution so the init-value comparison in `LoadDefaultFromInitValue` honours the market timezone rather than the host's local representation (`Clock_t.cs:44-49` already documents this gap). That is a *feature* — proper timezone modelling (per the house date/time convention, a NodaTime `DateTimeZone` at the boundary with an injected clock) — not a bug, and it is out of proportion to a TODO-clearance pass. The acceptance gate explicitly permits converting a TODO to a tracked issue with a justifying comment.

**Files:**
- Modify: `src/FixPortal.FixAtdl/Model/Controls/Clock_t.cs:35`

- [ ] **Step 1: Create the GitHub issue**

Run (single command; `--repo` per the fork caveat so `gh` does not target the upstream parent):

`gh issue create --repo FixPortal/fixportal-fixatdl --title "Clock_t: model LocalMktTz as a timezone type and apply it to initValue resolution" --body "Today Clock_t.LocalMktTz is a string and is stored but not applied: LoadDefaultFromInitValue compares initValue against the injected TimeProvider in the host's local representation (see Clock_t.cs remarks). Model LocalMktTz as a proper timezone type (NodaTime DateTimeZone at the boundary, per the house date/time convention) and apply it when resolving initValueMode==1. Deferred from Phase 2 (FixPortal.FixAtdl 1.0 roadmap) as a feature, not a bug."`

Capture the issue number/URL from the command output for Step 2.

> If `gh` is unauthenticated or offline at execution time, **stop and surface this** — the controller will create the issue (or authorise an alternative tracker) before the TODO is rewritten to reference a non-existent issue.

- [ ] **Step 2: Replace the TODO with the issue reference**

In `Clock_t.cs`, replace line 35:

```csharp
    // TODO: Implement LocalMktTz as a type.
```

with (substitute the real issue number from Step 1):

```csharp
    // LocalMktTz is stored but not yet applied to initValue resolution (timezone modelling deferred as a
    // feature, not a bug) — tracked in GitHub issue #<N>.
```

- [ ] **Step 3: Build clean and commit**

Run: `dotnet build D:\FixPortal\fixportal-fixatdl\FixPortal.FixAtdl.sln -c Release`
Expected: 0 errors, 0 new warnings.

```
git add src/FixPortal.FixAtdl/Model/Controls/Clock_t.cs
```
```
git commit -m "chore(controls): track Clock_t LocalMktTz typing as GitHub issue #<N>"
```

---

## Task 7: Record the disposition doc and verify the acceptance gate

**Files:**
- Create: `docs/batch-3-findings-disposition.md`

- [ ] **Step 1: Write the disposition doc**

Create `docs/batch-3-findings-disposition.md`:

```markdown
# Batch-3 Findings & TODO Disposition (Phase 2)

> Resolves the Phase 2 acceptance gate of the 1.0 roadmap: every in-`src/` TODO and every deferred
> batch-3 adversarial-review finding is fixed, closed with rationale, or tracked as an issue.
> Audit source: the batch-3 audit at
> `…/Adversarial Review/fixportal-fixatdl/full-audit-20260528T211015Z/`.

## Fixed
- **O-G2** — `EditValueConverter.ConvertToComparableType` now rejects a null operand with
  `InvalidFieldValueException` instead of silently coercing numerics to `0` (and NRE-ing on
  enum/MonthYear/Tenor). Guarded by `EditValueConverterTests`.
- **G-D** — `ThrowHelper` now threads the real `ParamName` through the ArgumentException-family path
  (default `"Value"` preserves existing callers). Guarded by `ThrowHelperTests`.

## Closed with rationale (no change)
- **F7 (TagNum leading zeros)** — kept lenient parse; enforcement-tightening, not a correctness defect;
  no conforming ATDL emits leading-zero tags. Pinned by characterization test in `TypeCoverageGapTests`.
- **#7 (Tenor non-positive offset)** — kept lenient parse; `D0` = same-day in some impls, offset-range is
  a business-layer concern. Pinned by characterization test in `TenorTests`.
- **G-G (IParentable.Parent nullability)** — kept non-nullable contract; making it nullable ripples to all
  five implementers and every consumer for a condition that cannot occur in a parsed model (parents are
  assigned at attach time). Cost > benefit for a Low finding on the eve of the 1.0 API freeze.
- **EditEvaluator / StateRule_t "Implement IDisposable"** — no disposable members, no event
  subscriptions; vestige of the removed Notification assembly (Task A8). TODOs replaced with rationale.
- **EditEvaluatingCollection "Unbind needed?"** — `Resolve` forwards only; no binding established. The
  dead `IBindable<T>` interface it referred to was removed.
- **ModelUtils "Move this somewhere better"** — `GetTypeFromName` belongs with the `_types` cache it
  reads; TODO removed.

## Deferred to a tracked issue
- **Clock_t LocalMktTz typing** — feature (NodaTime timezone modelling + initValue resolution), not a bug.
  GitHub issue #<N>.

## Already resolved in batch 3 (recorded for completeness)
- M4 — `ConvertToComparableType` wraps `FormatException`/`OverflowException` → `InvalidFieldValueException`
  (`EditValueConverter.cs:64-69`).
- L3 — `ConvertToBool` null guard + `ToUpperInvariant` (`EditValueConverter.cs:72-89`).
- #8 — `Tenor.Parse` catches `OverflowException` (`Tenor.cs:137`).
- `NonNegativeIntegerTypeBase` dead null-guard removed (`NonNegativeIntegerTypeBase.cs:50`).

## Out of scope for Phase 2
- `FixFieldValueProvider` percentage/enum-pair branch coverage and `ReadOnlyControlCollection`
  integration paths are *coverage* gaps (Phase 1 / Phase 3 review remit), not behaviour findings.
```

(Substitute the real issue number for `#<N>`.)

- [ ] **Step 2: Verify no stray TODO/FIXME/HACK/XXX remains in `src/`**

Run: `Grep` for `TODO|FIXME|HACK|XXX` in `src/`.
Expected: the only matches are the rationale comments added in Tasks 5/6 that no longer contain a bare `TODO` token (they were reworded). There must be **zero** `// TODO`, `FIXME`, `HACK`, or `XXX` markers. If any remain, resolve or convert-to-issue them before proceeding.

- [ ] **Step 3: Full green build + test + coverage floor**

Run: `dotnet build D:\FixPortal\fixportal-fixatdl\FixPortal.FixAtdl.sln -c Release`
Expected: 0 errors, 0 warnings (no new warnings vs the main baseline).
Run: `dotnet test D:\FixPortal\fixportal-fixatdl\FixPortal.FixAtdl.sln --collect:"XPlat Code Coverage" --results-directory D:\FixPortal\fixportal-fixatdl\coverage\phase2`
Expected: all tests PASS; coverage ≥ the 65% CI floor (it will be ≥ Phase 1's 69%, since Tasks 1-3 add tests and the source deltas are tiny).

- [ ] **Step 4: Commit the disposition doc**

```
git add docs/batch-3-findings-disposition.md
```
```
git commit -m "docs: record Phase 2 TODO + batch-3 finding dispositions"
```

---

## Phase 2 Acceptance Gate (from the roadmap)

- **Zero** `// TODO`/`FIXME`/`HACK`/`XXX` in `src/`, or each survivor converted to a tracked GitHub issue with a justifying comment. (Task 7 Step 2 enforces; Clock_t → issue #N is the only conversion.)
- Each batch-3 deferred item (O-G2, G-D, F7, #7, G-G) resolved or explicitly closed with rationale recorded in `docs/batch-3-findings-disposition.md`.
- Every behavioural change covered by a test (O-G2, G-D); closed findings pinned by characterization tests (F7, #7).
- Build 0 warnings / 0 errors; all tests green; CI green on the PR.

---

## Self-Review

**1. Spec coverage** — every Phase 2 scope item from the roadmap (lines 184-197) is mapped: the five `// TODO` sites → Tasks 5 (four) + 6 (Clock_t); the five named batch-3 deferred findings (G-D, G-G, O-G2, TagNum F7, Tenor #7) → Tasks 1-4. No scope dropped. The roadmap's extra candidates (`NonNegativeIntegerTypeBase` dead path, coverage gaps) are addressed as "already resolved" / "out of scope" with reasons, not silently ignored.

**2. Placeholder scan** — code-changing steps (Tasks 1, 2, 5, 6) carry exact file:line targets and full before/after code. The close-with-rationale tasks (3, 4) are concrete: 3 adds real characterization tests; 4 is explicitly documentation-only with its text in Task 7. The one genuine unknown — the precise `New<T>` overload signature for G-D — is handled by a read-first step plus an assertion-anchored test, not a vague instruction. `#<N>` is a deliberate runtime substitution (the issue number), flagged at every occurrence.

**3. Type/name consistency** — verified against source read while writing: `EditValueConverter.ConvertToComparableType(object, string)`, `ErrorMessages.IllegalUseOfNullError` (used at `EditValueConverter.cs:76`), `ThrowHelper.CreateException<T>` line 245 `["Value", message]`, `Tenor.Parse(string)`, `TagNum_t : NonZeroPositiveIntegerTypeBase`, `IBindable<T>` unused, the five `IParentable<T>` implementers, `Clock_t.LocalMktTz`. Test conventions match the existing `EditValueConverterTests` (xUnit v3, `.Should()`, global usings; explicit `using FixPortal.FixAtdl.Diagnostics.Exceptions;` already present in that file for `InvalidFieldValueException`).
