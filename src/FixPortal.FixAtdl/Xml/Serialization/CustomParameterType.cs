// FP Enhancement: 2026-09-12 — caller-supplied custom Parameter xsi:type registration.

using FixPortal.FixAtdl.Model.Types.Support;

namespace FixPortal.FixAtdl.Xml.Serialization;

/// <summary>
/// Registers a host-supplied CLR type for a custom (vendor-extension) <c>Parameter</c> <c>xsi:type</c>
/// that FIXatdl 1.1 permits beyond the standard type set, but which this library cannot itself define
/// (its semantics are vendor-specific by definition).
/// </summary>
public sealed record CustomParameterType
{
    /// <summary>Initializes a new <see cref="CustomParameterType"/>.</summary>
    /// <param name="xsiTypeName">The bare local name that appears in the ATDL document's <c>xsi:type</c>
    /// attribute (e.g. <c>"MyVendorCustomParam_t"</c>), with no namespace prefix.</param>
    /// <param name="clrType">The CLR type to instantiate for this <c>xsi:type</c>. Must implement
    /// <see cref="IParameterType"/> with a public parameterless constructor (the same contract
    /// <c>Parameter_t&lt;T&gt;</c> requires of every standard type), and is used directly - no
    /// <see cref="Type.GetType(string)"/> probing, so the type may live in any assembly.</param>
    /// <param name="attributes">The <c>xsi:type</c>-specific XML attribute mappings for this type (the
    /// same shape used for every standard type in <see cref="SchemaDefinitions"/>, e.g. a <c>maxValue</c>
    /// attribute mapped to a <c>Value.MaxValue</c> property path). Attributes common to every
    /// <c>Parameter</c> (<c>name</c>, <c>fixTag</c>, <c>use</c>, etc.) do not need repeating here.</param>
    /// <exception cref="ArgumentException"><paramref name="clrType"/> does not implement
    /// <see cref="IParameterType"/> or has no public parameterless constructor - checked eagerly here so a
    /// host learns of a mismatched registration immediately, rather than from a generic reflection failure
    /// deep inside deserialization the first time a document actually uses this <c>xsi:type</c>.</exception>
    public CustomParameterType(string xsiTypeName, Type clrType, ElementAttribute[] attributes)
    {
        if (!typeof(IParameterType).IsAssignableFrom(clrType) || clrType.GetConstructor(Type.EmptyTypes) is null)
        {
            throw new ArgumentException(
                $"'{clrType}' must implement {nameof(IParameterType)} and expose a public parameterless constructor.",
                nameof(clrType)
            );
        }

        XsiTypeName = xsiTypeName;
        ClrType = clrType;
        Attributes = attributes;
    }

    /// <summary>The bare local name that appears in the ATDL document's <c>xsi:type</c> attribute (e.g.
    /// <c>"MyVendorCustomParam_t"</c>), with no namespace prefix.</summary>
    public string XsiTypeName { get; }

    /// <summary>The CLR type to instantiate for this <c>xsi:type</c>.</summary>
    public Type ClrType { get; }

    /// <summary>The <c>xsi:type</c>-specific XML attribute mappings for this type.</summary>
    public ElementAttribute[] Attributes { get; }
}
