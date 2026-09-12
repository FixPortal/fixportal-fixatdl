using System.Text;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Utility;
using FixPortal.FixAtdl.Xml;
using NSubstitute;

namespace FixPortal.FixAtdl.Tests.Model.Elements;

/// <summary>
/// Proves Edit_t only decimal-parses a FIX_ field's value for comparison when the field's actual
/// FIX data type (from <see cref="FixFieldTypes"/>) is numeric. Before this, every FIX_ field was
/// decimal-parsed on a "does it look like a number" guess, which silently misclassified a
/// numeric-looking String/Char field (e.g. a zero-padded ClOrdID) as a number - "0001" and "1"
/// then compared equal instead of as distinct strings.
/// </summary>
public class EditFixFieldValueTypeTests
{
    private static Strategy_t LoadTwap()
    {
        string xml = FixtureFiles.ReadAllText("Fixtures/twap.xml");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new StrategiesReader().Load(stream).Strategies[0];
    }

    private static Edit_t<IParameter> MakeFixFieldEdit(Strategy_t strategy, string field, string value)
    {
        var edit = new Edit_t<IParameter>
        {
            Field = field,
            Operator = Operator_t.Equal,
            Value = value,
        };
        ((IResolvable<Strategy_t, IParameter>)edit).Resolve(strategy, strategy.Parameters);
        return edit;
    }

    [Fact]
    public void ClOrdID_zero_padded_wire_value_does_not_equal_its_unpadded_literal()
    {
        var strategy = LoadTwap();
        var initial = Substitute.For<IInitialFixValueProvider>();
        initial.InputFixValues.Returns(new FixTagValuesCollection { { 11, "0001" } });

        var edit = MakeFixFieldEdit(strategy, "FIX_ClOrdID", "1");
        edit.Evaluate(new FixFieldValueProvider(initial, strategy.Parameters));

        // ClOrdID (tag 11) is a FIX String field - "0001" and "1" must compare as distinct
        // strings, not as the equal number 1.
        edit.CurrentState.Should().BeFalse();
    }

    [Fact]
    public void ClOrdID_matching_wire_value_equals_its_literal()
    {
        var strategy = LoadTwap();
        var initial = Substitute.For<IInitialFixValueProvider>();
        initial.InputFixValues.Returns(new FixTagValuesCollection { { 11, "0001" } });

        var edit = MakeFixFieldEdit(strategy, "FIX_ClOrdID", "0001");
        edit.Evaluate(new FixFieldValueProvider(initial, strategy.Parameters));

        edit.CurrentState.Should().BeTrue();
    }

    [Fact]
    public void OrderQty_numeric_wire_value_still_compares_numerically()
    {
        var strategy = LoadTwap();
        var initial = Substitute.For<IInitialFixValueProvider>();
        initial.InputFixValues.Returns(new FixTagValuesCollection { { 38, "0100.0" } });

        // OrderQty (tag 38) is a FIX Qty field - the numeric leading/trailing zeros should not
        // matter for the comparison.
        var edit = MakeFixFieldEdit(strategy, "FIX_OrderQty", "100");
        edit.Evaluate(new FixFieldValueProvider(initial, strategy.Parameters));

        edit.CurrentState.Should().BeTrue();
    }

    [Fact]
    public void Field_valid_but_absent_from_the_type_dictionary_defaults_to_non_numeric()
    {
        var strategy = LoadTwap();
        var initial = Substitute.For<IInitialFixValueProvider>();

        // NoUsernames (tag 809) is a real FixField member but is the one tag FIX50SP2.xml's own
        // <fields> section omits, so it is absent from FixFieldTypes - a numeric-looking value on
        // an unclassified field must default to string comparison (the safe default), not be
        // decimal-parsed.
        initial.InputFixValues.Returns(new FixTagValuesCollection { { 809, "01" } });

        var edit = MakeFixFieldEdit(strategy, "FIX_NoUsernames", "1");
        edit.Evaluate(new FixFieldValueProvider(initial, strategy.Parameters));

        edit.CurrentState.Should().BeFalse();
    }

    [Fact]
    public void Field_name_that_is_not_a_real_FixField_member_is_unresolvable_regardless_of_type()
    {
        var strategy = LoadTwap();
        var initial = Substitute.For<IInitialFixValueProvider>();
        initial.InputFixValues.Returns([]);

        // "FIX_" plus a name that isn't a defined FixField member: TryGetValue already returns
        // false for this before any type classification runs, so the comparison finds no value on
        // either side and the edit does not evaluate true - unchanged from before this change.
        var edit = MakeFixFieldEdit(strategy, "FIX_NotARealFixField", "1");
        edit.Evaluate(new FixFieldValueProvider(initial, strategy.Parameters));

        edit.CurrentState.Should().BeFalse();
    }
}
