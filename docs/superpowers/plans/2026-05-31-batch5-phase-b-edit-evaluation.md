# Batch 5 — Phase B — Edit-Evaluation Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the four edit-evaluation conformance defects (H1, H2, M2, M3) so presence and comparison operators behave correctly against real broker ATDL.

**Architecture:** Four independent, surgical fixes in the model/validation layer, each locked with tests. H1 adds an `EnumState` presence predicate and routes EX/NX through it. H2 short-circuits a null right-hand operand symmetrically to the existing null-LHS guard. M2 rejects the mutually-exclusive `value`+`field2` combination at resolve time. M3 makes binary controls default to a concrete `false` at construction so `EQ "false"` StateRules fire deterministically. No public surface changes except one additive resource string.

**Tech Stack:** .NET 10, xUnit v3, AwesomeAssertions (`using AwesomeAssertions;` — NOT FluentAssertions), NSubstitute. The test project already wires these via `Directory.Packages.props`. ImplicitUsings is enabled (`System.Linq` etc. are global). IDE0045 (prefer conditional expression) is enforced as an **error** — avoid `if/else` assignment chains where a conditional expression is idiomatic.

**Branch / worktree:** All work happens in the batch-5 review worktree
`D:\fix-portal\fixportal-fixatdl\.claude\worktrees\reviewer-passes` on branch
`reviewer-findings-batch6` (already created from `origin/main` @ `265a308`).

**Design reference:** `docs/superpowers/specs/2026-05-31-batch5-conformance-fixes-design.md` §2.
**Findings reference:** `docs/batch-5-conformance-review.md` (H1/H2/M2/M3).

---

## File Structure

Production files modified:

- `src/FixPortal.FixAtdl/Model/Controls/Support/EnumState.cs` — add `HasSelection` predicate (H1).
- `src/FixPortal.FixAtdl/Model/Elements/Edit_t.cs` — `EvaluateExists` (H1), `EvaluateInequalityComparison` (H2), `Resolve` guard (M2).
- `src/FixPortal.FixAtdl/Model/Controls/Support/BinaryControlBase.cs` — field initializer `_value = false` (M3).
- `src/FixPortal.FixAtdl/Resources/ErrorMessages.resx` + `ErrorMessages.Designer.cs` — new `EditValueAndField2BothSet` string (M2).

Test files:

- `tests/FixPortal.FixAtdl.Tests/Model/Controls/ControlValueTests.cs` — append `EnumState.HasSelection` cases (H1) + `CheckBox` construction-default case (M3).
- `tests/FixPortal.FixAtdl.Tests/Validation/EditConformanceTests.cs` — **new file**, edit-level regressions for H1, H2, M2, M3.

Order: H1 → H2 → M2 → M3. Each task is self-contained and ends with a commit. The fixes do not depend on one another, so any task that goes BLOCKED can be skipped without breaking the others; note it and continue.

---

### Task 1: H1 — EX/NX for list controls

A list control (`DropDownList_t`, etc.) with nothing selected returns a non-null, all-false `EnumState` from `GetCurrentValue()`. `Edit_t.EvaluateExists` only treats `null` and `""` as absent, so an unselected list control reads as `EX ≡ true` / `NX ≡ false` — backwards. Add a presence predicate to `EnumState` and consult it in `EvaluateExists`.

**Files:**
- Modify: `src/FixPortal.FixAtdl/Model/Controls/Support/EnumState.cs` (add `HasSelection`)
- Modify: `src/FixPortal.FixAtdl/Model/Elements/Edit_t.cs:315-324` (`EvaluateExists`)
- Test: `tests/FixPortal.FixAtdl.Tests/Model/Controls/ControlValueTests.cs` (append to `EnumStateTests`)
- Test: `tests/FixPortal.FixAtdl.Tests/Validation/EditConformanceTests.cs` (new file)

**Key correctness note for the edit-level test:** after `StrategiesReader().Load(...)`, a list control's backing `EnumState` is still `null` (it is created in `LoadDefaultFromInitValue`, which runs only when the consumer calls `LoadDefaults`/`LoadInitValue`). If you evaluate EX/NX without first calling `LoadInitValue` on the control, `GetCurrentValue()` returns `null` and the test passes **vacuously** through the existing null path — NOT exercising the bug. You MUST call `dropdown.LoadInitValue(FixFieldValueProvider.Empty)` first so the all-false `EnumState` is materialised; that is the real bug condition.

- [ ] **Step 1: Write the failing EnumState unit tests**

Append these three facts to the `EnumStateTests` class in `tests/FixPortal.FixAtdl.Tests/Model/Controls/ControlValueTests.cs` (just before its closing brace):

```csharp
    [Fact]
    public void EnumState_has_selection_is_false_when_empty()
    {
        var state = new EnumState(["A", "B", "C"]);
        state.HasSelection.Should().BeFalse();
    }

    [Fact]
    public void EnumState_has_selection_is_true_when_a_bit_is_set()
    {
        var state = new EnumState(["A", "B", "C"]);
        state["B"] = true;
        state.HasSelection.Should().BeTrue();
    }

    [Fact]
    public void EnumState_has_selection_is_true_when_non_enum_value_set()
    {
        var state = new EnumState(["A", "B"]);
        state.NonEnumValue = "freetext";
        state.HasSelection.Should().BeTrue();
    }
```

- [ ] **Step 2: Write the failing edit-level test (new file)**

Create `tests/FixPortal.FixAtdl.Tests/Validation/EditConformanceTests.cs`:

```csharp
using System.Text;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Utility;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Validation;

/// <summary>
/// Conformance regressions for the batch-5 edit-evaluation findings (H1, H2, M2, M3),
/// driven against real broker-ATDL patterns.
/// </summary>
public class EditConformanceTests
{
    private static Strategy_t LoadFirst(string xml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new StrategiesReader().Load(stream).Strategies[0];
    }

    // ── H1 — EX/NX for list controls ─────────────────────────────────────────

    [Theory]
    [InlineData(Operator_t.NotExist, true)]
    [InlineData(Operator_t.Exist, false)]
    public async Task Unselected_list_control_reports_not_exists(Operator_t op, bool expected)
    {
        var xml = await File.ReadAllTextAsync("Fixtures/pov.xml", TestContext.Current.CancellationToken);
        var pov = LoadFirst(xml);

        // Materialise the (all-false) EnumState — without this GetCurrentValue() is null and the
        // test would pass vacuously through the existing null path rather than exercising the bug.
        var dropdown = pov.Controls["c_Aggression"];
        dropdown.LoadInitValue(FixFieldValueProvider.Empty);

        var edit = new Edit_t<Control_t> { Field = "c_Aggression", Operator = op };
        ((IResolvable<Strategy_t, Control_t>)edit).Resolve(pov, pov.Controls);
        edit.Evaluate();

        edit.CurrentState.Should().Be(expected);
    }

    [Theory]
    [InlineData(Operator_t.Exist, true)]
    [InlineData(Operator_t.NotExist, false)]
    public async Task Selected_list_control_reports_exists(Operator_t op, bool expected)
    {
        var xml = await File.ReadAllTextAsync("Fixtures/pov.xml", TestContext.Current.CancellationToken);
        var pov = LoadFirst(xml);

        var dropdown = pov.Controls["c_Aggression"];
        dropdown.LoadInitValue(FixFieldValueProvider.Empty);

        var selected = new EnumState(["PASSIVE", "NEUTRAL", "AGGRESSIVE"]);
        selected["NEUTRAL"] = true;
        dropdown.SetValue(selected);

        var edit = new Edit_t<Control_t> { Field = "c_Aggression", Operator = op };
        ((IResolvable<Strategy_t, Control_t>)edit).Resolve(pov, pov.Controls);
        edit.Evaluate();

        edit.CurrentState.Should().Be(expected);
    }
}
```

- [ ] **Step 3: Run the new tests to verify they fail**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~EditConformanceTests|FullyQualifiedName~EnumState_has_selection"`
Expected: the `EnumState_has_selection_*` facts FAIL to **compile** (no `HasSelection` member); the `Unselected_list_control_reports_not_exists` cases FAIL on assertion (EX returns true / NX returns false today). Because a compile failure blocks the whole run, it is acceptable to confirm failure after Step 4's `EnumState` change but before Step 5's `Edit_t` change — at that point `Unselected_*` must still fail on assertion. Either way, see a red signal before going green.

- [ ] **Step 4: Add the `HasSelection` predicate to `EnumState`**

In `src/FixPortal.FixAtdl/Model/Controls/Support/EnumState.cs`, add this property immediately after the `Count` property (around line 222):

```csharp
    /// <summary>
    /// Gets a value indicating whether this EnumState represents a real selection — i.e., at least one
    /// enumerated value is enabled, or a non-enum (free-text) value is present. An all-false EnumState
    /// with no non-enum value represents "nothing selected" and is treated as absent by EX/NX edits.
    /// </summary>
    public bool HasSelection => GetFirstSelectedEnumIdIndex() != -1 || !string.IsNullOrEmpty(_nonEnumValue);
```

- [ ] **Step 5: Route `EvaluateExists` through the presence predicate**

In `src/FixPortal.FixAtdl/Model/Elements/Edit_t.cs`, replace the body of `EvaluateExists` (lines 315-324):

```csharp
    private bool EvaluateExists(object value)
    {
        bool checkingForExist = Operator == Operator_t.Exist;

        // A list control returns a never-null EnumState; "nothing selected" is an all-false EnumState
        // (not null and not ""), so it must be treated as absent here or EX/NX would always be wrong
        // for list controls. Scalar/text/clock controls already return null when unset.
        bool empty = value == null
            || value as string == string.Empty
            || (value is EnumState enumState && !enumState.HasSelection);

        bool result = checkingForExist ? !empty : empty;

        return result;
    }
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~EditConformanceTests|FullyQualifiedName~EnumState_has_selection"`
Expected: PASS (the 3 `HasSelection` facts + the 4 EX/NX theory cases).

- [ ] **Step 7: Commit**

```
git add src/FixPortal.FixAtdl/Model/Controls/Support/EnumState.cs src/FixPortal.FixAtdl/Model/Elements/Edit_t.cs tests/FixPortal.FixAtdl.Tests/Model/Controls/ControlValueTests.cs tests/FixPortal.FixAtdl.Tests/Validation/EditConformanceTests.cs
git commit -m "fix(edits): EX/NX honour EnumState presence for list controls (H1)"
```

---

### Task 2: H2 — null RHS inequality is indeterminate

An inequality (`LT/LE/GT/GE`) whose right-hand operand is a missing FIX field resolves the RHS to `null`. The current `EvaluateInequalityComparison` short-circuits a null **LHS** to `false` but lets a null **RHS** fall through to `lhs.CompareTo(null)`, which returns `+1` for `decimal`/`DateTime` — fabricating a definite (and wrong) boolean from missing data. Short-circuit a null RHS to `false` symmetrically.

**Files:**
- Modify: `src/FixPortal.FixAtdl/Model/Elements/Edit_t.cs:337-367` (`EvaluateInequalityComparison`)
- Test: `tests/FixPortal.FixAtdl.Tests/Validation/EditConformanceTests.cs` (append)

- [ ] **Step 1: Write the failing test**

Append to `EditConformanceTests` (before the closing brace of the class):

```csharp
    // ── H2 — inequality against a missing FIX field is indeterminate ──────────

    [Theory]
    [InlineData(Operator_t.GreaterThan)]
    [InlineData(Operator_t.GreaterThanOrEqual)]
    [InlineData(Operator_t.LessThan)]
    [InlineData(Operator_t.LessThanOrEqual)]
    public async Task Inequality_against_missing_fix_field_is_false(Operator_t op)
    {
        var xml = await File.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadFirst(xml);
        twap.Parameters["Participation"].WireValue = "50";

        // Field2 is a FIX_ field that is never supplied → the RHS resolves to null.
        var edit = new Edit_t<IParameter>
        {
            Field = "Participation",
            Operator = op,
            Field2 = "FIX_DoesNotExist",
        };
        ((IResolvable<Strategy_t, IParameter>)edit).Resolve(twap, twap.Parameters);
        edit.Evaluate(FixFieldValueProvider.Empty);

        edit.CurrentState.Should().BeFalse();
    }
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~Inequality_against_missing_fix_field_is_false"`
Expected: FAIL — `GreaterThan` and `GreaterThanOrEqual` currently return `true` (50.CompareTo(null) == +1), so `CurrentState` is `true`, not the expected `false`.

- [ ] **Step 3: Add the null-RHS short-circuit**

In `src/FixPortal.FixAtdl/Model/Elements/Edit_t.cs`, replace the top of `EvaluateInequalityComparison` (lines 337-353) up to and including the type-mismatch guard:

```csharp
    private bool EvaluateInequalityComparison(IComparable lhs, IComparable rhs)
    {
        // It's not clear what the right thing is to do with a null LHS and an inequality operator
        // so we return false anyway
        if (lhs == null)
        {
            return false;
        }

        // A null RHS — e.g. an inequality against a missing FIX field — is an indeterminate
        // comparison, not an ordering. Short-circuit it to false symmetrically with the null-LHS
        // guard above, rather than letting lhs.CompareTo(null) fabricate a definite (+1) result.
        if (rhs == null)
        {
            return false;
        }

        // Operands of different runtime types (e.g. a decimal LHS vs a non-numeric string RHS, after
        // NormaliseNumericString) cannot be ordered: IComparable.CompareTo would throw a raw
        // ArgumentException. Surface a clear domain error instead.
        if (lhs.GetType() != rhs.GetType())
        {
            throw ThrowHelper.New<InvalidOperationException>(this, ErrorMessages.UnsupportedComparisonOperation, lhs, rhs);
        }
```

(The remainder of the method — `int compareResult = lhs.CompareTo(rhs);` through `return finalResult;` — is unchanged. Note the type-mismatch guard no longer needs its `rhs != null &&` clause because `rhs` is now guaranteed non-null at that point.)

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~Inequality_against_missing_fix_field_is_false"`
Expected: PASS (all four operator cases).

- [ ] **Step 5: Commit**

```
git add src/FixPortal.FixAtdl/Model/Elements/Edit_t.cs tests/FixPortal.FixAtdl.Tests/Validation/EditConformanceTests.cs
git commit -m "fix(edits): treat null RHS inequality as indeterminate (H2)"
```

---

### Task 3: M2 — reject both `value` and `field2`

`value` and `field2` are mutually-exclusive forms of an edit's right-hand side, but nothing forbids both. `GetRhsValue` silently prefers `value`, hiding the misconfiguration. Reject "both set" at resolve time with a precise error. Requires a new resource string.

**Files:**
- Modify: `src/FixPortal.FixAtdl/Resources/ErrorMessages.resx` (new `EditValueAndField2BothSet`)
- Modify: `src/FixPortal.FixAtdl/Resources/ErrorMessages.Designer.cs` (matching accessor)
- Modify: `src/FixPortal.FixAtdl/Model/Elements/Edit_t.cs` (`Resolve`, guard at top)
- Test: `tests/FixPortal.FixAtdl.Tests/Validation/EditConformanceTests.cs` (append)

- [ ] **Step 1: Write the failing test**

Append to `EditConformanceTests`:

```csharp
    // ── M2 — both 'value' and 'field2' set is rejected at resolve ─────────────

    [Fact]
    public async Task Edit_with_both_value_and_field2_is_rejected_on_resolve()
    {
        var xml = await File.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadFirst(xml);

        var edit = new Edit_t<IParameter>
        {
            Field = "Participation",
            Operator = Operator_t.Equal,
            Value = "50",
            Field2 = "FIX_OrderQty",
        };

        var act = () => ((IResolvable<Strategy_t, IParameter>)edit).Resolve(twap, twap.Parameters);

        act.Should().Throw<InconsistentStrategyException>();
    }
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~Edit_with_both_value_and_field2_is_rejected_on_resolve"`
Expected: FAIL — `Resolve` does not throw today (no `InconsistentStrategyException` raised).

- [ ] **Step 3: Add the resource string (resx)**

In `src/FixPortal.FixAtdl/Resources/ErrorMessages.resx`, insert this `<data>` element immediately after the `EditRefResolutionFailure` element (after its closing `</data>`, before `EnumerationNotFound`):

```xml
  <data name="EditValueAndField2BothSet" xml:space="preserve">
    <value>An Edit (Id '{0}') specifies both a 'value' and a 'field2' attribute; these are mutually exclusive forms of the right-hand side and may not both be supplied.</value>
  </data>
```

- [ ] **Step 4: Add the matching Designer accessor**

In `src/FixPortal.FixAtdl/Resources/ErrorMessages.Designer.cs`, insert this property immediately after the `EditRefResolutionFailure` accessor (after its closing brace at ~line 142, before the `EnumerationNotFound` summary block):

```csharp
        /// <summary>
        ///   Looks up a localized string similar to An Edit (Id &apos;{0}&apos;) specifies both a &apos;value&apos; and a &apos;field2&apos; attribute; these are mutually exclusive forms of the right-hand side and may not both be supplied..
        /// </summary>
        internal static string EditValueAndField2BothSet {
            get {
                return ResourceManager.GetString("EditValueAndField2BothSet", resourceCulture);
            }
        }
        
```

- [ ] **Step 5: Add the resolve-time guard**

In `src/FixPortal.FixAtdl/Model/Elements/Edit_t.cs`, in the explicit `IResolvable<Strategy_t, T>.Resolve` method, add this guard as the **first** statement (before the `(Edits as IResolvable<...>).Resolve(...)` call at line 473), so it fires before any field-source lookup:

```csharp
        // 'value' and 'field2' are mutually exclusive right-hand-side forms; both being present is a
        // malformed edit. Reject it here (fail fast at resolve) rather than silently letting 'value'
        // win in GetRhsValue. Both default to null! and are only non-null when the attribute was present.
        if (Value != null && Field2 != null)
        {
            throw ThrowHelper.New<InconsistentStrategyException>(this, ErrorMessages.EditValueAndField2BothSet, Id ?? "(unnamed)");
        }
```

`InconsistentStrategyException` is already in scope via `using FixPortal.FixAtdl.Diagnostics.Exceptions;` (used elsewhere in the file). `ThrowHelper` is aliased at the top of the file.

- [ ] **Step 6: Run the test to verify it passes**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~Edit_with_both_value_and_field2_is_rejected_on_resolve"`
Expected: PASS.

- [ ] **Step 7: Commit**

```
git add src/FixPortal.FixAtdl/Resources/ErrorMessages.resx src/FixPortal.FixAtdl/Resources/ErrorMessages.Designer.cs src/FixPortal.FixAtdl/Model/Elements/Edit_t.cs tests/FixPortal.FixAtdl.Tests/Validation/EditConformanceTests.cs
git commit -m "fix(edits): reject mutually-exclusive value+field2 at resolve (M2)"
```

---

### Task 4: M3 — unset binary control defaults to concrete `false`

A freshly-constructed `CheckBox_t`/`RadioButton_t` has `_value == null` until `LoadDefaults`/`LoadInitValue` runs. A StateRule keyed on `EQ "false"` (the broker-431 `EnableStartTime`/`EnableEndTime` pattern) therefore does not fire for a default/unset checkbox evaluated before initialisation — `AreEqual(null, "false")` is `false`. `LoadDefaultFromInitValue` already sets `false` when there is no `initValue`, so the residual gap is the construction-time default. Close it with a field initializer so the concrete-`false` guarantee holds regardless of when evaluation occurs.

**Files:**
- Modify: `src/FixPortal.FixAtdl/Model/Controls/Support/BinaryControlBase.cs:26` (`_value` field initializer)
- Test: `tests/FixPortal.FixAtdl.Tests/Model/Controls/ControlValueTests.cs` (append to `BinaryControlTests`)
- Test: `tests/FixPortal.FixAtdl.Tests/Validation/EditConformanceTests.cs` (append)

**Important — preserve three-state semantics:** `bool?` is deliberately tri-state (`true`/`false`/`null`, where `null` = "do not send over FIX"). This change alters only the *initial* default; `Reset()` (which sets `null`) and `SetValue((object)null)` MUST continue to yield `null`. The existing `CheckBox_reset_yields_null` and `CheckBox_set_value_null_sets_null` tests guard this — they must stay green.

- [ ] **Step 1: Write the failing tests**

Append to the `BinaryControlTests` class in `tests/FixPortal.FixAtdl.Tests/Model/Controls/ControlValueTests.cs`:

```csharp
    [Fact]
    public void CheckBox_defaults_to_concrete_false_before_load()
    {
        // M3: a freshly-constructed binary control must read as concrete false (not null) so an
        // EQ "false" StateRule fires deterministically even before LoadDefaults is called.
        var control = new CheckBox_t("chk");
        control.GetCurrentValue().Should().Be(false);
    }

    [Fact]
    public void RadioButton_defaults_to_concrete_false_before_load()
    {
        var control = new RadioButton_t("rb");
        control.GetCurrentValue().Should().Be(false);
    }
```

Append to `EditConformanceTests` the broker-431-pattern regression. This uses an inline strategy with an unbound `CheckBox_t` (matching broker-431, where `EnableStartTime` carries no `parameterRef`) and does NOT call `LoadDefaults`, so it exercises the construction-time default:

```csharp
    // ── M3 — EQ "false" fires for a default (unset) binary control ────────────

    private const string CheckBoxStrategyXml =
        """
        <?xml version="1.0" encoding="UTF-8"?>
        <Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core"
                    xmlns:val="http://www.fixprotocol.org/FIXatdl-1-1/Validation"
                    xmlns:lay="http://www.fixprotocol.org/FIXatdl-1-1/Layout"
                    xmlns:flow="http://www.fixprotocol.org/FIXatdl-1-1/Flow"
                    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                    strategyIdentifierTag="5001">
          <Strategy name="S" version="1" wireValue="S" uiRep="S" providerID="DEMO">
            <Parameter name="P" xsi:type="Int_t" fixTag="9001" use="optional"/>
            <lay:StrategyLayout>
              <lay:StrategyPanel title="P" orientation="VERTICAL" collapsible="false" border="Line">
                <lay:Control ID="EnableStartTime" xsi:type="lay:CheckBox_t" label=""/>
              </lay:StrategyPanel>
            </lay:StrategyLayout>
          </Strategy>
        </Strategies>
        """;

    [Fact]
    public void Eq_false_fires_for_default_unset_checkbox()
    {
        var strategy = LoadFirst(CheckBoxStrategyXml);

        // Deliberately do NOT call LoadDefaults — represents a default/unset checkbox.
        var edit = new Edit_t<Control_t>
        {
            Field = "EnableStartTime",
            Operator = Operator_t.Equal,
            Value = "false",
        };
        ((IResolvable<Strategy_t, Control_t>)edit).Resolve(strategy, strategy.Controls);
        edit.Evaluate();

        edit.CurrentState.Should().BeTrue();
    }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~defaults_to_concrete_false_before_load|FullyQualifiedName~Eq_false_fires_for_default_unset_checkbox"`
Expected: FAIL — `GetCurrentValue()` is `null` (so `.Should().Be(false)` fails), and the edit's `CurrentState` is `false` (because `AreEqual(null, "false")` is `false`).

> If `Eq_false_fires_for_default_unset_checkbox` fails instead with a **parse/load** error (unbound control rejected), bind the checkbox by adding `parameterRef="StartTimeEnabled"` to the control and declaring `<Parameter name="StartTimeEnabled" xsi:type="Boolean_t" fixTag="9001" use="optional"/>` in place of the `Int_t` param. broker-431 loads with unbound checkboxes, so the unbound form is expected to work; this is the documented fallback only.

- [ ] **Step 3: Add the field initializer**

In `src/FixPortal.FixAtdl/Model/Controls/Support/BinaryControlBase.cs`, change the `_value` field declaration (line 26) from:

```csharp
    protected bool? _value;
```

to:

```csharp
    // Default to a concrete false (not null) so an unset binary control reads as "false" for
    // EQ "false" StateRules even before LoadDefaults runs. Reset() still sets null deliberately
    // (null = "do not send over FIX"); the three-state contract is otherwise unchanged.
    protected bool? _value = false;
```

- [ ] **Step 4: Run the tests to verify they pass (and nothing regressed)**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~BinaryControlTests|FullyQualifiedName~Eq_false_fires_for_default_unset_checkbox"`
Expected: PASS — the two new construction-default facts and the edit regression pass, AND the pre-existing `CheckBox_reset_yields_null`, `CheckBox_set_value_null_sets_null`, and `CheckBox_no_init_value_defaults_to_false` facts remain green (confirming Reset/null semantics are preserved).

- [ ] **Step 5: Commit**

```
git add src/FixPortal.FixAtdl/Model/Controls/Support/BinaryControlBase.cs tests/FixPortal.FixAtdl.Tests/Model/Controls/ControlValueTests.cs tests/FixPortal.FixAtdl.Tests/Validation/EditConformanceTests.cs
git commit -m "fix(controls): binary controls default to concrete false (M3)"
```

---

## Final verification (after all tasks)

- [ ] **Full suite green:** `dotnet test tests/FixPortal.FixAtdl.Tests`
  Expected: all tests pass (Phase A left the suite at 594 green; this phase adds ~13 tests).
- [ ] **No new warnings:** `dotnet build src/FixPortal.FixAtdl/FixPortal.FixAtdl.csproj` produces no new compiler/analyzer warnings vs the batch-6 baseline (IDE0045 is an error — confirm none introduced).
- [ ] Dispatch the final holistic code review, then use **superpowers:finishing-a-development-branch** to open the Phase-B PR (`gh pr create --repo FixPortal/fixportal-fixatdl --base main`).

## Out of scope (deferred to later phases)

- C2's `UTCTimestamp_t` time-only-bound half, H3 (`EnumPair@index`), H4 (`definedByFIX`), M4 (rounding mode), and the two Phase-A follow-ups → **Phase C**.
- Broker-82/broker-431 fixtures + conformance tests (real client data — must obfuscate) → **Phase D**.
- N1/N2 are by-design (no change).
