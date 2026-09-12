// FP Enhancement: 2026-09-12 — caller-supplied custom Parameter xsi:type registration.

using FixPortal.FixAtdl.Model.Types.Support;

namespace FixPortal.FixAtdl.Xml.Serialization;

/// <summary>
/// Registers a host-supplied CLR type for a custom (vendor-extension) <c>Parameter</c> <c>xsi:type</c>
/// that FIXatdl 1.1 permits beyond the standard type set, but which this library cannot itself define
/// (its semantics are vendor-specific by definition).
/// </summary>
/// <param name="XsiTypeName">The bare local name that appears in the ATDL document's <c>xsi:type</c>
/// attribute (e.g. <c>"MyVendorCustomParam_t"</c>), with no namespace prefix.</param>
/// <param name="ClrType">The CLR type to instantiate for this <c>xsi:type</c>. Must implement
/// <see cref="IParameterType"/> with a public parameterless constructor (the same contract
/// <c>Parameter_t&lt;T&gt;</c> requires of every standard type), and is used directly - no
/// <see cref="Type.GetType(string)"/> probing, so the type may live in any assembly.</param>
/// <param name="Attributes">The <c>xsi:type</c>-specific XML attribute mappings for this type (the same
/// shape used for every standard type in <see cref="SchemaDefinitions"/>, e.g. a <c>maxValue</c> attribute
/// mapped to a <c>Value.MaxValue</c> property path). Attributes common to every <c>Parameter</c>
/// (<c>name</c>, <c>fixTag</c>, <c>use</c>, etc.) do not need repeating here.</param>
public sealed record CustomParameterType(string XsiTypeName, Type ClrType, ElementAttribute[] Attributes);
