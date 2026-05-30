using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Model.Reference;

namespace FixPortal.FixAtdl.Tests.Model.Elements;

/// <summary>
/// Property round-trip and basic behaviour tests for simple element classes.
/// These are primarily 0%-coverage classes: Country_t, Market_t, Region_t,
/// SecurityType_t, Description_t, ListItem_t, StateRule_t, StrategyEdit_t.
/// </summary>
public class ElementPropertyTests
{
    // ── ListItem_t ───────────────────────────────────────────────────────────

    [Fact]
    public void ListItem_t_ToString_returns_ui_rep()
    {
        var item = new ListItem_t { EnumId = "BUY", UiRep = "Buy", IsSelected = true };
        item.ToString().Should().Be("Buy");
    }

    [Fact]
    public void ListItem_t_properties_round_trip()
    {
        var item = new ListItem_t { EnumId = "SELL", UiRep = "Sell", IsSelected = false };
        item.EnumId.Should().Be("SELL");
        item.UiRep.Should().Be("Sell");
        item.IsSelected.Should().BeFalse();
    }

    [Fact]
    public void ListItem_t_IsSelected_defaults_to_false()
    {
        var item = new ListItem_t();
        item.IsSelected.Should().BeFalse();
    }

    // ── Edit_t (non-generic) ─────────────────────────────────────────────────

    [Fact]
    public void Edit_t_default_has_empty_child_edits()
    {
        var edit = new Edit_t();
        edit.Edits.Should().BeEmpty();
    }

    [Fact]
    public void Edit_t_properties_round_trip()
    {
        var edit = new Edit_t
        {
            Field = "Qty",
            Field2 = "MaxQty",
            Id = "e1",
            Operator = Operator_t.GreaterThan,
            LogicOperator = LogicOperator_t.And,
            Value = "500"
        };

        edit.Field.Should().Be("Qty");
        edit.Field2.Should().Be("MaxQty");
        edit.Id.Should().Be("e1");
        edit.Operator.Should().Be(Operator_t.GreaterThan);
        edit.LogicOperator.Should().Be(LogicOperator_t.And);
        edit.Value.Should().Be("500");
    }

    // ── StateRule_t ToString ─────────────────────────────────────────────────

    [Fact]
    public void StateRule_t_ToString_with_no_fields_set_shows_null_owner()
    {
        var rule = new StateRule_t();
        // _owner is null before parenting (_owner?.Id => empty), and no state fields set,
        // so ToString renders just the id fragment with an empty value.
        var result = rule.ToString();
        result.Should().Be("(Control.ID=\"\")");
    }

    [Fact]
    public void StateRule_t_ToString_with_all_fields_shows_all()
    {
        var rule = new StateRule_t
        {
            Enabled = true,
            Value = "42",
            Visible = false
        };

        var result = rule.ToString();
        result.Should().Contain("enabled=\"true\"");
        result.Should().Contain("value=\"42\"");
        result.Should().Contain("visible=\"false\"");
    }

    [Fact]
    public void StateRule_t_ToString_with_enabled_false_shows_false()
    {
        var rule = new StateRule_t { Enabled = false };
        rule.ToString().Should().Contain("enabled=\"false\"");
    }

    [Fact]
    public void StateRule_t_ToString_with_only_value_omits_enabled_and_visible()
    {
        var rule = new StateRule_t { Value = "hello" };
        var result = rule.ToString();
        result.Should().Contain("value=\"hello\"");
        result.Should().NotContain("enabled=");
        result.Should().NotContain("visible=");
    }

    [Fact]
    public void StateRule_t_properties_round_trip()
    {
        var rule = new StateRule_t { Enabled = true, Value = "X", Visible = true };
        rule.Enabled.Should().BeTrue();
        rule.Value.Should().Be("X");
        rule.Visible.Should().BeTrue();
    }

    // ── StrategyEdit_t ───────────────────────────────────────────────────────

    [Fact]
    public void StrategyEdit_t_InternalId_is_non_empty_guid_string()
    {
        var se = new StrategyEdit_t();
        se.InternalId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(se.InternalId, out _).Should().BeTrue();
    }

    [Fact]
    public void StrategyEdit_t_each_instance_gets_distinct_InternalId()
    {
        var a = new StrategyEdit_t();
        var b = new StrategyEdit_t();
        a.InternalId.Should().NotBe(b.InternalId);
    }

    [Fact]
    public void StrategyEdit_t_ErrorMessage_round_trips()
    {
        var se = new StrategyEdit_t { ErrorMessage = "Qty must be positive" };
        se.ErrorMessage.Should().Be("Qty must be positive");
    }

    // ── Country_t ────────────────────────────────────────────────────────────

    [Fact]
    public void Country_t_properties_round_trip()
    {
        var country = new Country_t
        {
            CountryCode = IsoCountryCode.GB,
            Inclusion = Inclusion_t.Include
        };
        country.CountryCode.Should().Be(IsoCountryCode.GB);
        country.Inclusion.Should().Be(Inclusion_t.Include);
    }

    [Fact]
    public void Country_t_exclusion_round_trips()
    {
        var country = new Country_t { CountryCode = IsoCountryCode.US, Inclusion = Inclusion_t.Exclude };
        country.Inclusion.Should().Be(Inclusion_t.Exclude);
        country.CountryCode.Should().Be(IsoCountryCode.US);
    }

    // ── Market_t ─────────────────────────────────────────────────────────────

    [Fact]
    public void Market_t_properties_round_trip()
    {
        var market = new Market_t { MICCode = "XLON", Inclusion = Inclusion_t.Include };
        market.MICCode.Should().Be("XLON");
        market.Inclusion.Should().Be(Inclusion_t.Include);
    }

    [Fact]
    public void Market_t_exclusion_round_trips()
    {
        var market = new Market_t { MICCode = "XNYS", Inclusion = Inclusion_t.Exclude };
        market.Inclusion.Should().Be(Inclusion_t.Exclude);
    }

    // ── Region_t ─────────────────────────────────────────────────────────────

    [Fact]
    public void Region_t_properties_round_trip()
    {
        var region = new Region_t { Name = Region.TheAmericas, Inclusion = Inclusion_t.Include };
        region.Name.Should().Be(Region.TheAmericas);
        region.Inclusion.Should().Be(Inclusion_t.Include);
    }

    [Fact]
    public void Region_t_Countries_collection_initialized_lazily()
    {
        var region = new Region_t();
        // Accessing Countries initializes it; should be non-null and empty
        region.Countries.Should().NotBeNull();
        region.Countries.Should().BeEmpty();
    }

    // ── SecurityType_t ───────────────────────────────────────────────────────

    [Fact]
    public void SecurityType_t_properties_round_trip()
    {
        var st = new SecurityType_t { Name = "OPT", Inclusion = Inclusion_t.Include };
        st.Name.Should().Be("OPT");
        st.Inclusion.Should().Be(Inclusion_t.Include);
    }

    [Fact]
    public void SecurityType_t_exclusion_round_trips()
    {
        var st = new SecurityType_t { Name = "CS", Inclusion = Inclusion_t.Exclude };
        st.Inclusion.Should().Be(Inclusion_t.Exclude);
    }

    // ── Description_t ────────────────────────────────────────────────────────

    [Fact]
    public void Description_t_Content_round_trips()
    {
        var d = new Description_t { Content = "This is a description." };
        d.Content.Should().Be("This is a description.");
    }

    [Fact]
    public void Description_t_implicit_cast_from_string()
    {
        Description_t d = "My description";
        d.Content.Should().Be("My description");
    }

    [Fact]
    public void Description_t_implicit_cast_to_string()
    {
        var d = new Description_t { Content = "Hello" };
        string s = d;
        s.Should().Be("Hello");
    }
}
