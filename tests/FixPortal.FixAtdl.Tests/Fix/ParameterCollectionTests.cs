using System.Text;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Model.Types;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Fix;

public class ParameterCollectionTests
{
    private static Strategies_t Load(string xml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new StrategiesReader().Load(stream);
    }

    private static async Task<Model.Collections.ParameterCollection> LoadTwapParametersAsync()
    {
        var xml = await File.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        return Load(xml).Strategies[0].Parameters;
    }

    [Fact]
    public async Task Load_initial_values_applies_matching_tags_and_preserves_missing_values_when_reset_is_false()
    {
        var parameters = await LoadTwapParametersAsync();
        parameters["EndTime"].WireValue = "20260101-16:00:00";

        FixTagValuesCollection initialValues = [];
        initialValues.Add(168, "20260101-09:30:00");

        parameters.LoadInitialValues(initialValues, resetNonSuppliedParameters: false);

        parameters["StartTime"].WireValue.Should().Be("20260101-09:30:00");
        parameters["EndTime"].WireValue.Should().Be("20260101-16:00:00");
        parameters["Participation"].IsSet.Should().BeFalse();
    }

    [Fact]
    public async Task Load_initial_values_resets_missing_values_when_requested()
    {
        var parameters = await LoadTwapParametersAsync();
        parameters["EndTime"].WireValue = "20260101-16:00:00";
        parameters["Participation"].WireValue = "0.1";

        FixTagValuesCollection initialValues = [];
        initialValues.Add(168, "20260101-09:30:00");

        parameters.LoadInitialValues(initialValues, resetNonSuppliedParameters: true);

        parameters["StartTime"].WireValue.Should().Be("20260101-09:30:00");
        parameters["EndTime"].IsSet.Should().BeFalse();
        parameters["Participation"].IsSet.Should().BeFalse();
    }

    [Fact]
    public void Get_output_values_omits_unset_and_untagged_parameters()
    {
        ParameterCollection parameters = [];
        var included = new Parameter_t<String_t>("Included") { FixTag = 168, WireValue = "20260101-09:30:00" };
        var unsetOptional = new Parameter_t<String_t>("UnsetOptional") { FixTag = 126, Use = Use_t.Optional };
        var localOnly = new Parameter_t<String_t>("LocalOnly") { WireValue = "internal" };

        parameters.Add(included);
        parameters.Add(unsetOptional);
        parameters.Add(localOnly);

        var values = parameters.GetOutputValues().ToDictionary(pair => (int)pair.Key, pair => pair.Value);

        values.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<int, string>(168, "20260101-09:30:00"));
    }

    [Fact]
    public async Task Get_output_values_throws_when_required_tagged_parameter_is_unset()
    {
        var parameters = await LoadTwapParametersAsync();
        parameters.Add(new Parameter_t<String_t>("LocalOnly") { WireValue = "internal" });

        var act = () => parameters.GetOutputValues();

        act.Should().Throw<MissingMandatoryValueException>();
    }

    [Fact]
    public async Task Reset_all_clears_existing_parameter_values()
    {
        var parameters = await LoadTwapParametersAsync();
        parameters["StartTime"].WireValue = "20260101-09:30:00";
        parameters["EndTime"].WireValue = "20260101-16:00:00";
        parameters["Participation"].WireValue = "0.1";

        parameters.ResetAll();

        parameters.Should().OnlyContain(parameter => !parameter.IsSet);
    }
}
