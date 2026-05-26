// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;
using System.Text;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Resources;
using ThrowHelper = FixPortal.FixAtdl.Diagnostics.ThrowHelper;

namespace FixPortal.FixAtdl.Fix;

/// <summary>
/// Represents a FIX message.
/// </summary>
public class FixMessage : Dictionary<FixField, string>
{
    /// <summary>Field separator.</summary>
    public static readonly char SOH = '\x01';

    /// <summary>Field/value separator.</summary>
    public static readonly char Separator = '=';

    /// <summary>
    /// Initializes a new instance of <see cref="FixMessage"/>.
    /// </summary>
    public FixMessage()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FixMessage"/> using the supplied FIX message.
    /// </summary>
    /// <param name="rawMessage">The FIX message to parse.</param>
    /// <remarks>The current implementation of this class does NOT support repeating blocks.</remarks>
    public FixMessage(string rawMessage)
    {
        if (string.IsNullOrEmpty(rawMessage))
        {
            throw ThrowHelper.New<FixParseException>(this, ErrorMessages.UnableToParseFixMessageEmpty);
        }

        string[] nameValuePairs = rawMessage.Split([SOH], StringSplitOptions.RemoveEmptyEntries);

        if (nameValuePairs.Length == 0)
        {
            throw ThrowHelper.New<FixParseException>(this, ErrorMessages.UnableToParseFixMessageInvalidContent, rawMessage);
        }

        string tagText = string.Empty;
        string valueText = string.Empty;

        try
        {
            foreach (string nameValuePair in nameValuePairs)
            {
                int separatorIndex = nameValuePair.IndexOf(Separator);

                if (separatorIndex <= 0 || separatorIndex == nameValuePair.Length - 1)
                {
                    throw ThrowHelper.New<FixParseException>(this, ErrorMessages.UnableToParseFixMessageInvalidContent, nameValuePair);
                }

                tagText = nameValuePair[..separatorIndex];
                valueText = nameValuePair[(separatorIndex + 1)..];

                int tag = Convert.ToInt32(tagText, CultureInfo.InvariantCulture);

                if (!TryAdd((FixField)tag, valueText))
                {
                    throw ThrowHelper.New<FixParseException>(this, ErrorMessages.UnableToParseFixMessageInvalidContent, nameValuePair);
                }
            }
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            throw ThrowHelper.New<FixParseException>(this, ex, ErrorMessages.UnableToParseFixMessageInvalidFormat, tagText, valueText, ex.Message);
        }
    }

    /// <summary>
    /// Gets the complete set of fix fields for this message.
    /// </summary>
    /// <value>The fix fields.</value>
    public ICollection<FixField> FixFields => Keys;

    /// <summary>
    /// Provides the string representation of this FixMessage.
    /// </summary>
    /// <returns>String representation of this message.</returns>
    /// <remarks>Emits fields in insertion order; FIX-spec ordering (header/trailer positions, repeating groups) is the responsibility of the host FIX engine.</remarks>
    public string ToFix()
    {
        StringBuilder sb = new();

        foreach (KeyValuePair<FixField, string> item in this)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}{2}{3}", ((uint)item.Key).ToString(CultureInfo.InvariantCulture), Separator, item.Value, SOH);
        }

        return sb.ToString();
    }
}
