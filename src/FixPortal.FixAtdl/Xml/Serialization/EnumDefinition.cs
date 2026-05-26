// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Xml.Serialization;

/// <summary>
/// Defines the textual representations used to map XML values onto an enum type.
/// </summary>
/// <param name="enumType">The enum type being mapped.</param>
/// <param name="textValues">The XML text-to-enum mapping.</param>
public class EnumDefinition(Type enumType, Dictionary<string, Enum> textValues)
{
    /// <summary>
    /// Gets the enum type being mapped.
    /// </summary>
    public Type EnumType { get; private set; } = enumType;

    /// <summary>
    /// Gets the XML text-to-enum mapping.
    /// </summary>
    public Dictionary<string, Enum> TextValues { get; private set; } = textValues;
}
