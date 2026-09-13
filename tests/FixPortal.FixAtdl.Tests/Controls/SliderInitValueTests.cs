using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Controls;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Enumerations;

namespace FixPortal.FixAtdl.Tests.Controls;

/// <summary>
/// R16: a numeric Slider_t (no ListItems) pre-seeds its inner spinner from initValue before the
/// UseFixField policy gets a chance to run. A malformed initValue must not abort that later policy.
/// </summary>
public class SliderInitValueTests
{
    [Fact]
    public void Malformed_initValue_does_not_abort_initialization_when_a_fix_field_supplies_the_value()
    {
        // initValue="abc" is not a number; with initPolicy="UseFixField" the FIX message's value must
        // still initialize the slider rather than the pre-seed throwing InvalidFieldValueException.
        FixTagValuesCollection fixValues = [];
        fixValues.Add(35, "42.5");
        var initialProvider = Substitute.For<IInitialFixValueProvider>();
        initialProvider.InputFixValues.Returns(fixValues);
        var provider = new FixFieldValueProvider(initialProvider, null);

        var slider = new Slider_t("s")
        {
            InitValue = "abc",
            InitPolicy = InitPolicy_t.UseFixField,
            InitFixField = "FIX_MsgType",
        };

        var act = () => slider.LoadInitValue(provider);

        act.Should().NotThrow();
        slider.GetCurrentValue().Should().Be(42.5m);
    }

    [Fact]
    public void Well_formed_initValue_still_pre_seeds_when_no_fix_field_is_supplied()
    {
        // The guard only skips unparseable initValues; a valid one keeps its pre-seed behavior.
        var slider = new Slider_t("s") { InitValue = "12.5" };

        slider.LoadInitValue(FixFieldValueProvider.Empty);

        slider.GetCurrentValue().Should().Be(12.5m);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-number")]
    public void A_later_LoadInitValue_without_a_parseable_initValue_does_not_restore_an_earlier_seed(
        string? laterInitValue
    )
    {
        // CR6: the inner spinner kept the InitValue an earlier LoadInitValue seeded, so a second call
        // whose initValue was null or malformed restored the previous parse as its fallback value.
        var slider = new Slider_t("s") { InitValue = "12.5" };
        slider.LoadInitValue(FixFieldValueProvider.Empty);
        slider.GetCurrentValue().Should().Be(12.5m);

        slider.InitValue = laterInitValue!;
        slider.LoadInitValue(FixFieldValueProvider.Empty);

        slider.GetCurrentValue().Should().BeNull();
    }
}
