using System.Globalization;
using System.Text.Json;
using AwesomeAssertions;
using FixPortal.FixAtdl.Contracts;
using FixPortal.FixAtdl.Contracts.StateRules;
using Xunit;

namespace FixPortal.FixAtdl.Contracts.Tests;

/// <summary>
/// Data-driven tests for <see cref="StateRuleEvaluator"/> driven from the shared
/// <c>contracts/state-rule-cases.json</c> corpus. The same JSON is consumed by the
/// TypeScript evaluator test so both implementations must agree on every case.
/// </summary>
public class StateRuleEvaluatorTests
{
    public static IEnumerable<object[]> Cases() =>
        StateRuleCorpus
            .Load(Path.Join(AppContext.BaseDirectory, "contracts/state-rule-cases.json"))
            .Select(c => new object[] { c.Name, c.Ast, c.FormState, c.Expected });

    // and_short_circuit_skips_invalid_data_operand and or_short_circuit_skips_invalid_data_operand
    // pin the C# short-circuit result. React currently returns null for the same cases.
    [Theory, MemberData(nameof(Cases))]
    public void Evaluator_matches_corpus(
        string name,
        StateRuleAstNodeDto ast,
        IReadOnlyDictionary<string, object?> formState,
        bool expected
    )
    {
        var sut = new StateRuleEvaluator();
        sut.Evaluate(ast, formState).Should().Be(expected, name);
    }

    // -------------------------------------------------------------------------
    // Extra [Fact] tests for edge cases that are awkward in the corpus
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("==")]
    [InlineData("!=")]
    [InlineData("<")]
    public void Data_comparison_is_rejected_even_under_not(string op)
    {
        var leaf = new StateRuleAstNodeDto("compare", op, "a", "abc", null, ComparisonType: "Data_t");
        var act = () =>
            new StateRuleEvaluator().Evaluate(
                StateRuleAst.Not(leaf),
                new Dictionary<string, object?> { ["a"] = "abc" }
            );
        act.Should().Throw<AtdlParseException>().Which.Code.Should().Be(AtdlParseExceptionCode.InvalidEditValue);
    }

    [Theory]
    [InlineData("Tenor_t", "D0")]
    [InlineData("MonthYear_t", "20260230")]
    [InlineData("TZTimeOnly_t", "bad-time")]
    [InlineData("TZTimestamp_t", "bad-timestamp")]
    public void Invalid_domain_operand_is_rejected(string type, string value)
    {
        var leaf = new StateRuleAstNodeDto("compare", "==", "a", value, null, ComparisonType: type);
        var act = () => new StateRuleEvaluator().Evaluate(leaf, new Dictionary<string, object?> { ["a"] = value });
        act.Should().Throw<AtdlParseException>().Which.Code.Should().Be(AtdlParseExceptionCode.InvalidEditValue);
    }

    [Fact]
    public void Evaluate_null_ast_throws_ArgumentNullException()
    {
        var sut = new StateRuleEvaluator();
        var act = () => sut.Evaluate(null!, new Dictionary<string, object?>());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Evaluate_unknown_kind_throws_AtdlParseException_with_correct_code()
    {
        // An AST node with an unrecognised kind must be rejected rather than silently
        // returning a wrong answer.
        var sut = new StateRuleEvaluator();
        var unknownNode = new StateRuleAstNodeDto("unknown-kind", null, null, null, null);

        var act = () => sut.Evaluate(unknownNode, new Dictionary<string, object?>());

        act.Should().Throw<AtdlParseException>().Which.Code.Should().Be(AtdlParseExceptionCode.UnknownStateRuleKind);
    }

    [Fact]
    public void Not_with_zero_children_throws()
    {
        var sut = new StateRuleEvaluator();
        var badNot = new StateRuleAstNodeDto(StateRuleAstKind.Not, null, null, null, []);

        var act = () => sut.Evaluate(badNot, new Dictionary<string, object?>());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Bool_form_value_satisfies_string_equality_against_lowercase_literal()
    {
        // A CLR bool field value ("True"/"False" via ToString()) must still satisfy a
        // FIXatdl "true"/"false" string comparison — not fall through to case-sensitive
        // ordinal mismatch.
        var sut = new StateRuleEvaluator();
        var ast = new StateRuleAstNodeDto(StateRuleAstKind.Compare, StateRuleOperator.Eq, "c_Flag", "true", null);
        var formState = new Dictionary<string, object?> { ["c_Flag"] = true };

        sut.Evaluate(ast, formState).Should().BeTrue();
    }

    [Fact]
    public void Deeply_nested_AND_chain_throws_MaxDepthExceeded_instead_of_overflowing_stack()
    {
        // Build a 200-deep chain of single-child AND nodes — well past the 64-level guard.
        StateRuleAstNodeDto current = new(StateRuleAstKind.Compare, StateRuleOperator.Eq, "c_Leaf", "1", null);
        for (var i = 0; i < 200; i++)
        {
            current = new StateRuleAstNodeDto(StateRuleAstKind.And, null, null, null, [current]);
        }

        var sut = new StateRuleEvaluator();
        var act = () => sut.Evaluate(current, new Dictionary<string, object?> { ["c_Leaf"] = "1" });

        act.Should().Throw<AtdlParseException>().Which.Code.Should().Be(AtdlParseExceptionCode.MaxDepthExceeded);
    }

    [Theory]
    [InlineData("1,000", "1000")]
    [InlineData("1000+", "1000")]
    [InlineData("1000-", "-1000")]
    [InlineData("1E2", "100")]
    public void Untyped_text_rejects_non_fix_decimal_spellings(string field, string literal)
    {
        Evaluate("==", field, literal).Should().BeFalse();
    }

    [Fact]
    public void Numeric_text_orders_against_non_numeric_text()
    {
        Evaluate("<", "123", "NONE").Should().BeTrue();
        Evaluate("==", "123", "NONE").Should().BeFalse();
        Evaluate(">", "10", "9").Should().BeTrue();
    }

    [Fact]
    public void Clock_accepts_minute_precision_that_utc_date_rejects()
    {
        Evaluate("==", "09:30", "09:30:00", "Clock_t").Should().BeTrue();
        Reject("==", "00:00:00", "20200102", "UTCDateOnly_t");
        Reject("!=", "bad-date", "20200102", "UTCDateOnly_t");
    }

    [Theory]
    [InlineData("UTCDateOnly_t", "==", "00:00:00", "20200102")]
    [InlineData("UTCDateOnly_t", "!=", "bad-date", "20200102")]
    [InlineData("LocalMktDate_t", "!=", "bad-date", "20200102")]
    [InlineData("UTCTimestamp_t", "==", "00:00:00", "20200102-00:00:00")]
    [InlineData("UTCTimeOnly_t", "!=", "20200102", "00:00:00")]
    public void Strict_temporal_operands_reject_the_wrong_wire_shape(
        string type,
        string op,
        string field,
        string literal
    )
    {
        Reject(op, field, literal, type);
    }

    [Fact]
    public void Utc_date_orders_valid_wire_values() =>
        Evaluate("<", "20200102", "20200103", "UTCDateOnly_t").Should().BeTrue();

    [Fact]
    public void String_comparison_uses_invariant_text()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Evaluate("==", 1.5d, "1.5", "String_t").Should().BeTrue();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Text_ordering_does_not_compare_selection_arrays() =>
        Evaluate("<", new object?[] { "B" }, "T", "String_t").Should().BeFalse();

    private static bool Evaluate(string op, object? field, object? literal, string? comparisonType = null) =>
        new StateRuleEvaluator().Evaluate(
            new StateRuleAstNodeDto("compare", op, "a", literal, null, ComparisonType: comparisonType),
            new Dictionary<string, object?> { ["a"] = field }
        );

    private static void Reject(string op, object? field, object? literal, string comparisonType)
    {
        var act = () => Evaluate(op, field, literal, comparisonType);
        act.Should().Throw<AtdlParseException>().Which.Code.Should().Be(AtdlParseExceptionCode.InvalidEditValue);
    }
}

// =============================================================================
// Corpus loader — small static helper, lives alongside the tests that consume it.
// The TypeScript test reads the same shared contract artifact directly via the file system.
// =============================================================================

/// <summary>
/// One entry from <c>state-rule-cases.json</c>.
/// </summary>
public sealed record StateRuleCorpusCase(
    string Name,
    StateRuleAstNodeDto Ast,
    // Exposed as IReadOnlyDictionary to satisfy the evaluator signature. The loader
    // produces a concrete dictionary rather than a collection expression, which the
    // C# 12 parser can misread next to a generic parameter.
    IReadOnlyDictionary<string, object?> FormState,
    bool Expected
);

/// <summary>
/// Loads and deserialises <c>state-rule-cases.json</c>.
/// </summary>
public static class StateRuleCorpus
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public static IReadOnlyList<StateRuleCorpusCase> Load(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var raw =
            JsonSerializer.Deserialize<RawCase[]>(json, JsonOpts)
            ?? throw new InvalidOperationException("state-rule-cases.json deserialised to null.");

        var result = new List<StateRuleCorpusCase>(raw.Length);
        foreach (var r in raw)
        {
            result.Add(new StateRuleCorpusCase(r.Name, r.Ast, UnwrapFormState(r.FormState), r.Expected));
        }
        return result;
    }

    // System.Text.Json deserialises object values as JsonElement. Unwrap them to
    // primitives so the evaluator never needs to know about JsonElement.
    // Null JSON values remain null. Strings stay strings. Booleans stay bool.
    // Numbers are promoted to decimal so numeric comparisons work uniformly.
    private static Dictionary<string, object?> UnwrapFormState(Dictionary<string, object?> raw)
    {
        var result = new Dictionary<string, object?>(raw.Count, StringComparer.Ordinal);
        foreach (var (key, value) in raw)
        {
            result[key] = UnwrapElement(value);
        }

        return result;
    }

    private static object? UnwrapElement(object? value)
    {
        if (value is not JsonElement el)
        {
            return value;
        }

        return el.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.GetDecimal(),
            JsonValueKind.Array => el.EnumerateArray().Select(item => UnwrapElement(item)).ToArray(),
            _ => el.GetRawText(), // fallback; not expected in this corpus
        };
    }

    // Private raw type used purely for JSON deserialisation. A positional record so the
    // deserializer assigns every member, including the boolean, through the constructor.
    private sealed record RawCase(
        string Name,
        StateRuleAstNodeDto Ast,
        Dictionary<string, object?> FormState,
        bool Expected
    );
}
