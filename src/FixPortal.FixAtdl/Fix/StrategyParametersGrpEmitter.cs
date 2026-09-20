using System.Globalization;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Resources;
using ThrowHelper = FixPortal.FixAtdl.Diagnostics.ThrowHelper;

namespace FixPortal.FixAtdl.Fix;

/// <summary>
/// Emits the FIX StrategyParametersGrp tag sequence (957-960) for a strategy's current parameter
/// values, for hosts using <see cref="Strategies_t.Tag957Support"/> transport instead of (or in
/// addition to) direct per-parameter FIX tags.
/// </summary>
/// <remarks>
/// Tag reference:
///   957 NoStrategyParameters — count of filled parameter repetitions.
///   958 StrategyParameterName — parameter name string.
///   959 StrategyParameterType — FIX 5.0 SP2 data type code (int); the tag has no FIX 4.4
///   enumeration.
///   960 StrategyParameterValue — the parameter's wire value.
///
/// Parameters that are not set (<see cref="IParameter.IsSet"/> is false, or
/// <see cref="IParameter.WireValue"/> is null) are silently skipped; the 957 count reflects only
/// the set parameters. Tag 957 itself is omitted entirely when no parameters are set — an empty
/// repeating group should not appear on the wire at all.
/// </remarks>
public static class StrategyParametersGrpEmitter
{
    /// <summary>Produces the FIX StrategyParametersGrp tag list for the strategy's current parameter values.</summary>
    public static IReadOnlyList<(int Tag, string Value)> Emit(Strategy_t strategy)
    {
        ArgumentNullException.ThrowIfNull(strategy);

        // Pre-collect in parameter declaration order so the 957 count is known before emission.
        // An empty WireValue is filtered alongside a missing one: emitting "960=" would produce a
        // field the parser rejects (Low 6).
        var filled = new List<IParameter>(strategy.Parameters.Count);
        foreach (var parameter in strategy.Parameters)
        {
            if (parameter.IsSet && !string.IsNullOrEmpty(parameter.WireValue))
            {
                filled.Add(parameter);
            }
        }

        var result = new List<(int, string)>(filled.Count == 0 ? 0 : 1 + filled.Count * 3);
        if (filled.Count > 0)
        {
            result.Add((957, filled.Count.ToString(CultureInfo.InvariantCulture)));
        }

        foreach (var parameter in filled)
        {
            // Parameter names come from broker-supplied ATDL XML and reach the wire unvalidated — nothing
            // upstream constrains them the way String_t constrains a value. A name (or a wire value, for a
            // host that joins these tuples itself rather than routing them through FixMessage.ToFix)
            // carrying SOH would split one field into two and inject arbitrary FIX fields. Guard both at
            // this single emission chokepoint, mirroring FixMessage.ToFix.
            if (parameter.Name.Contains(FixMessage.SOH))
            {
                throw ThrowHelper.New<InvalidOperationException>(
                    strategy,
                    ErrorMessages.ValueContainsDelimiter,
                    "StrategyParameterName (tag 958)"
                );
            }

            if (parameter.WireValue!.Contains(FixMessage.SOH))
            {
                throw ThrowHelper.New<InvalidOperationException>(
                    strategy,
                    ErrorMessages.ValueContainsDelimiter,
                    "StrategyParameterValue (tag 960)"
                );
            }

            result.Add((958, parameter.Name));
            result.Add(
                (959, FixStrategyParameterTypeCodes.Resolve(parameter.Type).ToString(CultureInfo.InvariantCulture))
            );
            result.Add((960, parameter.WireValue));
        }

        return result;
    }
}
