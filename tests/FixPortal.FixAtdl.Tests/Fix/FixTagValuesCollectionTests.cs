using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Fix;

namespace FixPortal.FixAtdl.Tests.Fix;

public class FixTagValuesCollectionTests
{
    // Construction -----------------------------------------------------------

    [Fact]
    public void Default_constructor_creates_empty_collection()
    {
        FixTagValuesCollection col = [];
        col.Should().BeEmpty();
    }

    [Fact]
    public void Collection_initializer_syntax_then_add_works()
    {
        FixTagValuesCollection col = [];
        col.Add(168, "20260101-09:30:00");
        col.Should().ContainSingle();
    }

    [Fact]
    public void Static_Empty_returns_new_instance_each_time()
    {
        var a = FixTagValuesCollection.Empty;
        var b = FixTagValuesCollection.Empty;
        a.Should().NotBeSameAs(b);
    }

    [Fact]
    public void Static_Empty_starts_with_no_entries()
    {
        FixTagValuesCollection.Empty.Should().BeEmpty();
    }

    // Add + indexer get ------------------------------------------------------

    [Fact]
    public void Add_and_FixField_indexer_retrieves_value()
    {
        FixTagValuesCollection col = [];
        col.Add(168, "09:30:00");

        col[(FixField)168].Should().Be("09:30:00");
    }

    [Fact]
    public void Add_and_string_indexer_retrieves_value_by_field_name()
    {
        FixTagValuesCollection col = [];
        col.Add(168, "09:30:00");

        // FixField enum member name for tag 168 is FIX_EffectiveTime
        col["FIX_EffectiveTime"].Should().Be("09:30:00");
    }

    [Fact]
    public void String_indexer_setter_updates_value()
    {
        FixTagValuesCollection col = [];
        col.Add(168, "original");
        col["FIX_EffectiveTime"] = "updated";

        col["FIX_EffectiveTime"].Should().Be("updated");
    }

    [Fact]
    public void FixField_indexer_setter_updates_value()
    {
        FixTagValuesCollection col = [];
        col.Add(168, "original");
        col[(FixField)168] = "updated";

        col[(FixField)168].Should().Be("updated");
    }

    // Duplicate guard --------------------------------------------------------

    [Fact]
    public void Add_duplicate_tag_throws_FixParseException()
    {
        FixTagValuesCollection col = [];
        col.Add(168, "first");

        var act = () => col.Add(168, "second");
        act.Should().Throw<FixParseException>();
    }

    // TryGetValue ------------------------------------------------------------

    [Fact]
    public void TryGetValue_by_tag_returns_true_and_value_when_present()
    {
        FixTagValuesCollection col = [];
        col.Add(168, "val");

        col.TryGetValue((FixTag)168, out var v).Should().BeTrue();
        v.Should().Be("val");
    }

    [Fact]
    public void TryGetValue_by_tag_returns_false_when_absent()
    {
        FixTagValuesCollection col = [];

        col.TryGetValue((FixTag)168, out _).Should().BeFalse();
    }

    [Fact]
    public void TryGetValue_by_string_returns_true_and_value_when_present()
    {
        FixTagValuesCollection col = [];
        col.Add(168, "val");

        col.TryGetValue("FIX_EffectiveTime", out var v).Should().BeTrue();
        v.Should().Be("val");
    }

    [Fact]
    public void TryGetValue_by_string_returns_false_for_unknown_field_name()
    {
        FixTagValuesCollection col = [];

        col.TryGetValue("NotAFixField", out var v).Should().BeFalse();
        v.Should().BeNull();
    }

    [Fact]
    public void TryGetValue_by_string_returns_false_when_field_defined_but_not_in_collection()
    {
        FixTagValuesCollection col = [];

        // FIX_EffectiveTime (168) is a valid FixField but not added
        col.TryGetValue("FIX_EffectiveTime", out _).Should().BeFalse();
    }

    // ToFix / round-trip ------------------------------------------------------

    [Fact]
    public void ToFix_produces_soh_delimited_tag_equals_value_pairs()
    {
        FixTagValuesCollection col = [];
        col.Add(35, "D");
        col.Add(49, "SENDER");

        var fix = col.ToFix();

        fix.Should().Contain($"35{FixMessage.Separator}D{FixMessage.SOH}");
        fix.Should().Contain($"49{FixMessage.Separator}SENDER{FixMessage.SOH}");
    }

    [Fact]
    public void Round_trip_via_string_constructor_preserves_values()
    {
        FixTagValuesCollection original = [];
        original.Add(35, "D");
        original.Add(49, "SENDER");

        var wire = original.ToFix();
        var parsed = new FixTagValuesCollection(wire);

        parsed.TryGetValue((FixTag)35, out var v35).Should().BeTrue();
        v35.Should().Be("D");
        parsed.TryGetValue((FixTag)49, out var v49).Should().BeTrue();
        v49.Should().Be("SENDER");
    }

    [Fact]
    public void Constructor_from_FixMessage_wraps_existing_message()
    {
        var msg = new FixMessage($"35{FixMessage.Separator}D{FixMessage.SOH}");
        var col = new FixTagValuesCollection(msg);

        col.TryGetValue((FixTag)35, out var v).Should().BeTrue();
        v.Should().Be("D");
    }

    // ToString ---------------------------------------------------------------

    [Fact]
    public void ToString_replaces_soh_with_pipe_for_display()
    {
        FixTagValuesCollection col = [];
        col.Add(35, "D");

        col.ToString().Should().Contain(" | ");
        col.ToString().Should().NotContain(FixMessage.SOH.ToString());
    }

    // Enumeration ------------------------------------------------------------

    [Fact]
    public void Enumeration_yields_all_added_pairs()
    {
        FixTagValuesCollection col = [];
        col.Add(35, "D");
        col.Add(49, "SENDER");

        var pairs = col.ToList();
        pairs.Should().HaveCount(2);
    }
}
