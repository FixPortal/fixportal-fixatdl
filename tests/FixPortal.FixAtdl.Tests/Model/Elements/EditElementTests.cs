using System.Text;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Utility;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Model.Elements;

/// <summary>
/// Tests for Edit_t (non-generic) construction, Edit_t&lt;T&gt; paths not covered
/// elsewhere (ToString, Sources, missing-operator throw), and EditRef_t&lt;T&gt;
/// resolution + delegation paths.
/// </summary>
public class EditElementTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Strategy_t LoadTwap(string xml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new StrategiesReader().Load(stream).Strategies[0];
    }

    private static Edit_t<IParameter> MakeResolvedEdit(
        Strategy_t twap, string field, Operator_t op, string? value = null)
    {
        var edit = new Edit_t<IParameter> { Field = field, Operator = op, Value = value };
        ((IResolvable<Strategy_t, IParameter>)edit).Resolve(twap, twap.Parameters);
        return edit;
    }

    // ── Edit_t (non-generic) defaults ────────────────────────────────────────

    [Fact]
    public void Edit_t_nongeneric_all_nullable_props_are_null_by_default()
    {
        var edit = new Edit_t();
        edit.Field.Should().BeNull();
        edit.Field2.Should().BeNull();
        edit.Id.Should().BeNull();
        edit.Operator.Should().BeNull();
        edit.LogicOperator.Should().BeNull();
        edit.Value.Should().BeNull();
    }

    [Fact]
    public void Edit_t_nongeneric_Edits_is_initialized_and_empty()
    {
        var edit = new Edit_t();
        edit.Edits.Should().NotBeNull();
        edit.Edits.Should().BeEmpty();
    }

    // ── Edit_t<T> ToString paths ─────────────────────────────────────────────

    [Fact]
    public void Edit_t_generic_ToString_with_no_properties_returns_empty_parens()
    {
        var edit = new Edit_t<IParameter>();
        edit.ToString().Should().Be("()");
    }

    [Fact]
    public void Edit_t_generic_ToString_with_all_set_fields_formats_correctly()
    {
        var edit = new Edit_t<IParameter>
        {
            Id = "e1",
            Field = "Participation",
            Operator = Operator_t.Equal,
            Value = "100",
            Field2 = "BenchmarkPrice"
        };

        var result = edit.ToString();
        result.Should().Contain("Id=\"e1\"");
        result.Should().Contain("Field=\"Participation\"");
        result.Should().Contain("Operator=\"Equal\"");
        result.Should().Contain("Value=\"100\"");
        result.Should().Contain("Field2=\"BenchmarkPrice\"");
    }

    [Fact]
    public void Edit_t_generic_ToString_with_logic_operator_shows_logic_operator()
    {
        var edit = new Edit_t<IParameter> { LogicOperator = LogicOperator_t.And };
        edit.ToString().Should().Contain("LogicOperator=\"And\"");
    }

    // ── Edit_t<T> initial state for StrategyEdit (IParameter) ────────────────

    [Fact]
    public void Edit_t_IParameter_CurrentState_defaults_to_true_before_evaluate()
    {
        // For StrategyEdits (T=IParameter), the spec says the initial state is "valid" (true)
        var edit = new Edit_t<IParameter>();
        edit.CurrentState.Should().BeTrue();
    }

    // ── Edit_t<T> missing-operators throws ───────────────────────────────────

    [Fact]
    public async Task Edit_t_Evaluate_with_neither_operator_nor_logic_operator_throws_InvalidOperationException()
    {
        var xml = await File.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadTwap(xml);

        // Edit with no Operator and no LogicOperator
        var edit = new Edit_t<IParameter> { Field = "Participation" };
        ((IResolvable<Strategy_t, IParameter>)edit).Resolve(twap, twap.Parameters);

        var act = () => edit.Evaluate();

        act.Should().Throw<InvalidOperationException>();
    }

    // ── Edit_t<T> Sources property ───────────────────────────────────────────

    [Fact]
    public async Task Sources_with_operator_and_field_only_contains_field()
    {
        var xml = await File.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadTwap(xml);

        var edit = MakeResolvedEdit(twap, "Participation", Operator_t.Exist);

        edit.Sources.Should().ContainSingle().Which.Should().Be("Participation");
    }

    [Fact]
    public async Task Sources_with_FIX_prefixed_field_is_included_in_sources()
    {
        var xml = await File.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadTwap(xml);

        // FIX_ prefix fields are resolved via FixFieldValueProvider, not Parameters
        var edit = new Edit_t<IParameter>
        {
            Field = "FIX_OrderQty",
            Operator = Operator_t.Exist
        };
        ((IResolvable<Strategy_t, IParameter>)edit).Resolve(twap, twap.Parameters);

        edit.Sources.Should().Contain("FIX_OrderQty");
    }

    // ── EditRef_t<T> pre-resolve throws InternalErrorException ───────────────

    [Fact]
    public void EditRef_t_Evaluate_before_Resolve_throws_InternalErrorException()
    {
        var editRef = new EditRef_t<IParameter>("editA");

        var act = () => editRef.Evaluate();

        act.Should().Throw<InternalErrorException>();
    }

    [Fact]
    public void EditRef_t_accessing_Field_before_Resolve_throws_InternalErrorException()
    {
        var editRef = new EditRef_t<IParameter>("editA");

        var act = () => { _ = editRef.Field; };

        act.Should().Throw<InternalErrorException>();
    }

    [Fact]
    public void EditRef_t_ToString_before_Resolve_returns_empty_string()
    {
        var editRef = new EditRef_t<IParameter>("myEdit");
        editRef.ToString().Should().BeEmpty();
    }

    [Fact]
    public void EditRef_t_Id_is_set_by_constructor()
    {
        var editRef = new EditRef_t<IParameter>("editX");
        editRef.Id.Should().Be("editX");
    }

    // ── EditRef_t<T> unknown-id throws ReferencedObjectNotFoundException ──────

    [Fact]
    public async Task EditRef_t_Resolve_with_unknown_id_throws_ReferencedObjectNotFoundException()
    {
        var xml = await File.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadTwap(xml);

        var editRef = new EditRef_t<IParameter>("nonExistentEdit");

        var act = () => ((IResolvable<Strategy_t, IParameter>)editRef)
            .Resolve(twap, twap.Parameters);

        act.Should().Throw<ReferencedObjectNotFoundException>();
    }

    // ── EditRef_t<T> successful resolution and delegation ────────────────────

    [Fact]
    public async Task EditRef_t_after_resolve_delegates_Evaluate_to_referenced_edit()
    {
        var xml = await File.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadTwap(xml);

        // Inject a named Edit_t (non-generic) into the strategy's Edits collection.
        // EditCollection is a KeyedCollection keyed on Edit_t.Id.
        var sourceEdit = new Edit_t
        {
            Id = "e_part",
            Field = "Participation",
            Operator = Operator_t.Equal,
            Value = "50"
        };
        twap.Edits.Add(sourceEdit);

        // Set the parameter value so evaluation succeeds.
        twap.Parameters["Participation"].WireValue = "50";

        var editRef = new EditRef_t<IParameter>("e_part");
        ((IResolvable<Strategy_t, IParameter>)editRef).Resolve(twap, twap.Parameters);

        editRef.Evaluate();

        editRef.CurrentState.Should().BeTrue();
    }

    [Fact]
    public async Task EditRef_t_after_resolve_ToString_shows_referenced_edit_details()
    {
        var xml = await File.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadTwap(xml);

        var sourceEdit = new Edit_t
        {
            Id = "e_str",
            Field = "Participation",
            Operator = Operator_t.Exist
        };
        twap.Edits.Add(sourceEdit);

        var editRef = new EditRef_t<IParameter>("e_str");
        ((IResolvable<Strategy_t, IParameter>)editRef).Resolve(twap, twap.Parameters);

        // After resolve, ToString delegates to the cloned Edit_t<T>
        var result = editRef.ToString();
        result.Should().NotBeEmpty();
        result.Should().Contain("Participation");
    }

    [Fact]
    public async Task EditRef_t_evaluate_with_false_result_reflects_in_CurrentState()
    {
        var xml = await File.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadTwap(xml);

        var sourceEdit = new Edit_t
        {
            Id = "e_false",
            Field = "Participation",
            Operator = Operator_t.Equal,
            Value = "999"
        };
        twap.Edits.Add(sourceEdit);

        // Participation set to something other than 999
        twap.Parameters["Participation"].WireValue = "50";

        var editRef = new EditRef_t<IParameter>("e_false");
        ((IResolvable<Strategy_t, IParameter>)editRef).Resolve(twap, twap.Parameters);

        editRef.Evaluate();

        editRef.CurrentState.Should().BeFalse();
    }
}
