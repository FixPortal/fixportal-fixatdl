# FixPortal.FixAtdl.Contracts

The JSON contract consumed by
[`@fix-portal/fixatdl-react`](https://github.com/FixPortal/fixportal-fixatdl-react),
produced from a FIXatdl document parsed by
[`FixPortal.FixAtdl`](https://github.com/FixPortal/fixportal-fixatdl).

It contains:

- the DTO records the React package renders (`AtdlStrategiesDto`, `AtdlStrategyDto`,
  `AtdlParameterDto`, `AtdlPanelDto`, `AtdlControlDto`, `StateRuleAstNodeDto`, ...);
- `AtdlDtoMapper`, which maps a parsed `Strategies_t` to those records, including
  each control's state rules as an AST;
- `StateRuleEvaluator`, which evaluates the same AST server-side, so a host can
  apply the rules the form applied;
- `AtdlContractJson.Options`, the serializer settings the React package expects
  (camelCase, null members omitted).

## Usage

```shell
dotnet add package FixPortal.FixAtdl.Contracts
```

```csharp
using System.Text.Json;
using FixPortal.FixAtdl.Contracts;
using FixPortal.FixAtdl.Xml;

using var stream = File.OpenRead("strategies.xml");
var strategies = new StrategiesReader().Load(stream);

AtdlStrategiesDto contract = new AtdlDtoMapper().Map(strategies);
string json = JsonSerializer.Serialize(contract, AtdlContractJson.Options);
```

Serve `json` to the browser and pass a strategy from it to the React form.

`Map` takes an optional `IReadOnlyDictionary<string, string>` of source XML per
strategy name, which populates each strategy's `sourceXml`. Without it,
`sourceXml` is an empty string; the form does not need it.

Mapping throws `AtdlParseException` for a document whose state rules cannot be
built (an unresolved `EditRef`, an unknown operator, an invalid literal, a
strategy edit with neither `Edit` nor `EditRef`, or nesting deeper than the
supported limit). `Code` carries a machine-readable reason from
`AtdlParseExceptionCode`.

A compare against a non-numeric `FIX_` field carries `comparisonType` `String_t`,
so a zero-padded identifier is not compared as a number. A numeric `FIX_` field
omits `comparisonType` and still compares as a number. A compare between a
`Boolean_t` parameter and another field carries `trueWireValue` and
`falseWireValue` (default `Y` and `N`); `StateRuleEvaluator` matches those tokens
with an ordinal comparison. `AND` and `OR` return as soon as the result is known.
The corpus cases `and_short_circuit_skips_invalid_data_operand` and
`or_short_circuit_skips_invalid_data_operand` pin that result for C#. The React
evaluator does not yet.

## State-rule corpus

The evaluator is held to the shared corpus
[`contracts/state-rule-cases.json`](https://github.com/FixPortal/fixportal-fixatdl/blob/main/contracts/state-rule-cases.json),
the same cases the React package's TypeScript evaluator runs against.

## Licence

MIT. See [LICENSE](https://github.com/FixPortal/fixportal-fixatdl/blob/main/LICENSE).
