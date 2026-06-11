// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Collections;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Resources;
using FixPortal.FixAtdl.Utility;
using ThrowHelper = FixPortal.FixAtdl.Diagnostics.ThrowHelper;

namespace FixPortal.FixAtdl.Fix;

/// <summary>
/// Represents a collection of FIX tag values backed by a <see cref="FixMessage"/>.
/// </summary>
public class FixTagValuesCollection : IEnumerable<KeyValuePair<FixField, string>>
{
    private readonly FixMessage _message;

    /// <summary>
    /// Initializes a new empty <see cref="FixTagValuesCollection"/>.
    /// </summary>
    public FixTagValuesCollection()
    {
        _message = [];
    }

    /// <summary>
    /// Initializes a new <see cref="FixTagValuesCollection"/> from a FIX wire-format message.
    /// </summary>
    /// <param name="fixMessage">The FIX message to parse.</param>
    public FixTagValuesCollection(string fixMessage)
        : this(new FixMessage(fixMessage))
    {
    }

    /// <summary>
    /// Initializes a new <see cref="FixTagValuesCollection"/> from an existing <see cref="FixMessage"/>.
    /// </summary>
    /// <param name="message">The backing FIX message.</param>
    public FixTagValuesCollection(FixMessage message)
    {
        _message = message;
    }

    /// <summary>
    /// Gets a fresh empty collection instance.
    /// </summary>
    /// <remarks>Returns a new instance on each access rather than a shared singleton: the indexer
    /// setter and Add mutate the backing message, so a shared Empty could bleed values across
    /// independent evaluations.</remarks>
    public static FixTagValuesCollection Empty => [];

    /// <summary>
    /// Gets or sets the value for the specified FIX field.
    /// </summary>
    /// <param name="fixField">The FIX field to read or write.</param>
    public string this[FixField fixField]
    {
        get => _message[fixField]; set => _message[fixField] = value;
    }

    /// <summary>
    /// Gets or sets the value for the specified FIX field name.
    /// </summary>
    /// <param name="fixField">The FIX field name.</param>
    public string this[string fixField]
    {
        get
        {
            FixField field = fixField.ParseAsEnum<FixField>();

            return _message[field];
        }

        set
        {
            FixField field = fixField.ParseAsEnum<FixField>();

            _message[field] = value;
        }
    }

    /// <summary>
    /// Attempts to get the value for the specified FIX field name.
    /// </summary>
    /// <param name="fixField">The FIX field name.</param>
    /// <param name="value">When this method returns, contains the field value if found.</param>
    /// <returns><see langword="true"/> if the field was present; otherwise, <see langword="false"/>.</returns>
    public bool TryGetValue(string fixField, out string value)
    {
        // Honour the Try-pattern: an unknown/extension/symbolic field name returns false rather than
        // throwing out of ParseAsEnum.
        if (!Enum.TryParse(fixField, true, out FixField field))
        {
            value = null!;
            return false;
        }

        bool result = _message.TryGetValue(field, out string? v);
        value = v!;
        return result;
    }

    /// <summary>
    /// Attempts to get the value for the specified FIX tag.
    /// </summary>
    /// <param name="tag">The FIX tag to look up.</param>
    /// <param name="value">When this method returns, contains the field value if found.</param>
    /// <returns><see langword="true"/> if the field was present; otherwise, <see langword="false"/>.</returns>
    public bool TryGetValue(FixTag tag, out string value)
    {
        FixField field = tag;

        bool result = _message.TryGetValue(field, out string? v);
        value = v!;
        return result;
    }

    /// <summary>
    /// Adds a FIX tag value to the collection.
    /// </summary>
    /// <param name="tag">The FIX tag to add.</param>
    /// <param name="value">The tag value.</param>
    public void Add(FixTag tag, string value)
    {
        // Use TryAdd so a duplicate tag surfaces as a domain FixParseException, consistent with the
        // FixMessage(string) parse path, rather than a raw Dictionary ArgumentException.
        if (!_message.TryAdd(tag, value))
        {
            throw ThrowHelper.New<FixParseException>(this, ErrorMessages.AttemptToAddDuplicateKey, ((FixField)tag).ToString(), "FixMessage");
        }
    }

    /// <summary>
    /// Converts the collection to FIX wire format.
    /// </summary>
    /// <returns>The FIX message using SOH delimiters.</returns>
    public string ToFix()
    {
        return _message.ToFix();
    }

    /// <summary>
    /// Returns a readable string form of the FIX message.
    /// </summary>
    /// <returns>The FIX message with SOH delimiters replaced for display.</returns>
    public override string ToString()
    {
        try
        {
            return ToFix().Replace("\x01", " | ");
        }
        catch (Exception ex)
        {
            return $"[Invalid FIX Message: {ex.Message}]";
        }
    }

    /// <summary>
    /// Returns an enumerator over the tag-value pairs.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    public IEnumerator<KeyValuePair<FixField, string>> GetEnumerator()
    {
        return _message.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _message.GetEnumerator();
    }
}
