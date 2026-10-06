namespace FixPortal.FixAtdl.Contracts;

/// <summary>
/// Thrown when a FIXatdl document cannot be parsed or mapped to the contract.
/// <see cref="Code"/> carries a machine-readable reason.
/// </summary>
public sealed class AtdlParseException : Exception
{
    /// <summary>Gets the machine-readable error code for this parse failure.</summary>
    public string Code { get; }

    /// <summary>
    /// Initialises a new <see cref="AtdlParseException"/> with a code and a human-readable message.
    /// </summary>
    public AtdlParseException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>
    /// Initialises a new <see cref="AtdlParseException"/> wrapping a lower-level exception.
    /// </summary>
    public AtdlParseException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }
}

/// <summary>
/// Machine-readable error codes carried by <see cref="AtdlParseException"/>.
/// </summary>
public static class AtdlParseExceptionCode
{
    /// <summary>The document is not well-formed XML (XmlException from the parser).</summary>
    public const string MalformedXml = "ATDL_MALFORMED_XML";

    /// <summary>
    /// The document is well-formed XML but the root element is not a valid FIXatdl Strategies
    /// element in the expected namespace.
    /// </summary>
    public const string NotStrategiesRoot = "ATDL_NOT_STRATEGIES_ROOT";

    /// <summary>
    /// The document is structurally valid but failed during resolution (e.g. bad cross-references
    /// between strategies, parameters, or controls).
    /// </summary>
    public const string ParseFailed = "ATDL_PARSE_FAILED";

    /// <summary>
    /// An EditRef in a StateRule could not be resolved against the strategy-level or global Edit
    /// registries.  Indicates a FIXatdl document that references an Edit ID that was never defined.
    /// </summary>
    public const string UnresolvedEditRef = "ATDL_UNRESOLVED_EDIT_REF";

    /// <summary>
    /// A StateRule Edit carries an Operator or LogicOperator value that is not part of the
    /// known FIXatdl operator set, indicating a document that extends or mis-spells an operator.
    /// </summary>
    public const string UnknownStateRuleOperator = "ATDL_UNKNOWN_STATE_RULE_OPERATOR";

    /// <summary>An Edit literal does not match the referenced parameter's value format.</summary>
    public const string InvalidEditValue = "ATDL_INVALID_EDIT_VALUE";

    /// <summary>
    /// A StateRule AST node carries a <c>Kind</c> value that is not recognised by the evaluator.
    /// Indicates either a document that was authored against a newer schema version or a
    /// programming error in the builder.
    /// </summary>
    public const string UnknownStateRuleKind = "ATDL_UNKNOWN_STATE_RULE_KIND";

    /// <summary>
    /// Nesting (panels, StateRule Edits, or StateRule AST evaluation) exceeded the maximum
    /// supported depth. Guards against unbounded recursion / stack overflow from a maliciously
    /// or accidentally deeply-nested document.
    /// </summary>
    public const string MaxDepthExceeded = "ATDL_MAX_DEPTH_EXCEEDED";
}
