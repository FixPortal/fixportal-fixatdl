namespace FixPortal.FixAtdl.Diagnostics.Exceptions;

/// <summary>
/// The exception that is thrown when a FIXatdl document fails validation against a caller-supplied
/// <see cref="System.Xml.Schema.XmlSchemaSet"/>. Distinct from <see cref="ValidationException"/>, which
/// covers parameter constraint / StrategyEdit rule failures at the model level rather than document structure.
/// </summary>
public class SchemaValidationException : FixAtdlException
{
    /// <summary>
    /// The individual schema validation error messages, in document order.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaValidationException"/> class.
    /// </summary>
    /// <param name="errors">The individual schema validation error messages, in document order.</param>
    public SchemaValidationException(IReadOnlyList<string> errors)
        : base(BuildMessage(errors))
    {
        Errors = errors;
    }

    private static string BuildMessage(IReadOnlyList<string> errors) =>
        $"The FIXatdl document failed schema validation with {errors.Count} error(s): {string.Join(" | ", errors)}";
}
