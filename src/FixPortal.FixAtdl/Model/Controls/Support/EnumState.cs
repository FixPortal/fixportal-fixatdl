// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Collections;
using System.Globalization;
using System.Text;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Resources;
using ThrowHelper = FixPortal.FixAtdl.Diagnostics.ThrowHelper;

namespace FixPortal.FixAtdl.Model.Controls.Support;

/// <summary>
/// Represents the state of an enumerated value within FIX, i.e., one that may have a single value, that maps to, say, Char_t, or
/// one that may have several values, which in FIX is typed as a MultipleStringValue.
/// </summary>
/// <remarks>During its existence, this type has at times been a class and at other times a struct.  It has settled on being
/// a class because its key function is to represent changing state, and mutable structs are seen to be <i>a bad thing</i>.
/// As it is a class, the <see cref="Copy"/> method is included to provide a means to copy the contents in order to avoid
/// the scenario that two independent entities share a reference to a given EnumState.</remarks>
public class EnumState
{
    private const string ExceptionContext = "EnumState";

    private readonly BitArray _enumStates;
    private readonly string[] _enumIds;
    private string? _nonEnumValue;

    /// <summary>
    /// Initializes a new instance of <see cref="EnumState"/> with the supplied set of EnumID values.
    /// </summary>
    /// <param name="enumIds">Array of EnumID values.  The order of this array should match the order of the
    /// EnumPair elements within the target parameter.</param>
    public EnumState(string[] enumIds)
    {
        // Guard up front: Count, the indexer and ToString all dereference _enumIds unconditionally,
        // so a null array would surface as an opaque NRE far from here.
        _enumIds = enumIds ?? throw ThrowHelper.New<ArgumentNullException>(typeof(EnumState), "A valid array of EnumIDs must be supplied.");
        _enumStates = new BitArray(_enumIds.Length);
        _nonEnumValue = null;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="EnumState"/> and copies the data from the supplied EnumState.
    /// </summary>
    /// <param name="sourceState">EnumState to copy the initial state from.</param>
    public EnumState(EnumState sourceState)
    {
        if (sourceState == null)
        {
            throw ThrowHelper.New<ArgumentException>(typeof(EnumState), "A valid EnumState value must be supplied to use this constuctor");
        }

        _enumIds = sourceState._enumIds;
        _enumStates = new BitArray(sourceState._enumStates);
        _nonEnumValue = sourceState._nonEnumValue;
    }

    /// <summary>
    /// Makes a copy (deep clone) of this EnumState.
    /// </summary>
    /// <remarks>This method is provided to allow value type-like semantics for EnumState.</remarks>
    public EnumState Copy()
    {
        return new EnumState(this);
    }

    /// <summary>
    /// Updates this EnumState with the state of the supplied EnumState.
    /// </summary>
    /// <param name="source">Source EnumState to update this instance from.</param>
    /// <remarks>Implementation note: there are probably more effective ways to copy data from one BitArray to another that the one taken, but
    /// the current implementation ensures that the two EnumStates reference a common set of EnumIDs.</remarks>
    public void UpdateFrom(EnumState source)
    {
        if (source == null)
        {
            throw ThrowHelper.New<ArgumentNullException>(this, "A valid EnumState must be supplied");
        }

        if (_enumIds.Length != source._enumIds.Length)
        {
            throw ThrowHelper.New<ArgumentException>(this, "Unable to update this EnumState from supplied EnumState as the number of EnumIDs was not consistent");
        }

        // Validate that every EnumID lines up BEFORE mutating any bit, so a mismatch leaves this
        // instance untouched rather than partially copied (Theme D — no partial mutation before throw).
        for (int n = 0; n < _enumIds.Length; n++)
        {
            if (Array.IndexOf(source._enumIds, _enumIds[n]) < 0)
            {
                throw ThrowHelper.New<ArgumentException>(this, "Mismatch between the EnumIDs of the source and target EnumState");
            }
        }

        for (int n = 0; n < _enumIds.Length; n++)
        {
            int index = Array.IndexOf(source._enumIds, _enumIds[n]);

            _enumStates.Set(n, source._enumStates[index]);
        }

        _nonEnumValue = source._nonEnumValue;
    }

    /// <summary>
    /// Determines whether the object supplied has the identical state to this instance.
    /// </summary>
    /// <param name="obj">Object to compare this object to.</param>
    /// <returns>true if the state of the supplied object is identical to this object instance; false otherwise.</returns>
    /// <remarks>This method assumes that both operands have the same set of EnumID values.</remarks>
    public override bool Equals(object? obj)
    {
        if (obj == null || obj is not EnumState state)
        {
            return false;
        }

        // Compare the bit states element-wise. The previous _enumStates.Equals(state) compared a
        // BitArray to an EnumState by reference, so it was ALWAYS false for two distinct instances,
        // breaking equality / HashSet / Dictionary / dirty-checking semantics.
        return _nonEnumValue == state._nonEnumValue && BitArraysEqual(_enumStates, state._enumStates);
    }

    private static bool BitArraysEqual(BitArray left, BitArray right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        for (int n = 0; n < left.Length; n++)
        {
            if (left[n] != right[n])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Serves as a hash function for this type.  Overridden because Equals(object) is overridden.
    /// </summary>
    /// <returns>A hash code for the current Object.</returns>
    /// <remarks>The value 251 is used here because it is a prime number, helpful for generating unique hash values.</remarks>
    public override int GetHashCode()
    {
        unchecked // No issue with int overflow
        {
            // Size the backing int[] for the full bit count: BitArray.CopyTo into new int[1] throws
            // ArgumentException once there are more than 32 bits (e.g. a long MultiSelectList).
            int wordCount = Math.Max(1, (_enumStates.Length + 31) / 32);
            int[] enumStates = new int[wordCount];
            _enumStates.CopyTo(enumStates, 0);

            // Hash only the state and the non-enum value — the same fields Equals compares. The old
            // code mixed in _enumIds.GetHashCode() (a reference/identity hash), which made two equal
            // states with distinct _enumIds arrays hash differently, violating the Equals/GetHashCode
            // contract.
            int hashCode = 17;

            foreach (int word in enumStates)
            {
                hashCode = hashCode * 251 + word;
            }

            if (_nonEnumValue != null)
            {
                hashCode = hashCode * 251 + _nonEnumValue.GetHashCode(StringComparison.Ordinal);
            }

            return hashCode;
        }
    }

    /// <summary>
    /// Gets/sets the boolean state of the enumerated element specified by the supplied EnumID value.
    /// </summary>
    /// <param name="enumId">EnumID to get/set the boolean state for.</param>
    /// <returns>Boolean state for the specified EnumID.</returns>
    public bool this[string enumId]
    {
        get
        {
            for (int n = 0; n < _enumIds.Length; n++)
            {
                if (_enumIds[n] == enumId)
                {
                    return _enumStates[n];
                }
            }

            throw ThrowHelper.New<ArgumentException>(this, ErrorMessages.UnrecognisedEnumIdValue, enumId);
        }

        set
        {
            for (int n = 0; n < _enumIds.Length; n++)
            {
                if (_enumIds[n] == enumId)
                {
                    _enumStates[n] = value;

                    return;
                }
            }

            throw ThrowHelper.New<ArgumentException>(this, ErrorMessages.UnrecognisedEnumIdValue, enumId);
        }
    }

    /// <summary>
    /// Gets the number of elements (EnumIDs) that this EnumState holds state for.
    /// </summary>
    public int Count => _enumIds.Length;

    /// <summary>
    /// Determines whether the supplied EnumID value is valid for this EnumState instance.
    /// </summary>
    /// <param name="enumId">EnumID value to evaluate.</param>
    /// <returns>true if the supplied EnumID is valid for this EnumState; false otherwise.</returns>
    public bool IsValidEnumId(string enumId)
    {
        return Array.Exists(_enumIds, s => s == enumId);
    }

    internal bool Matches(string enumId)
    {
        if (!IsValidEnumId(enumId))
        {
            throw ThrowHelper.New<InvalidFieldValueException>(this, ErrorMessages.UnrecognisedEnumIdValue, enumId);
        }

        return this[enumId];
    }

    /// <summary>
    /// Gets/sets the non-enum value; the non-enum value is only used by the EditableDropDownList_t type.
    /// </summary>
    public string? NonEnumValue
    {
        get => _nonEnumValue;

        set
        {
            _enumStates.SetAll(false);

            _nonEnumValue = value;
        }
    }

    /// <summary>
    /// Gets the first EnumID that has a value of true.
    /// </summary>
    /// <returns>The first EnumID if any enumerated values are set; otherwise an empty string is returned.</returns>
    public string GetFirstSelectedEnumId()
    {
        int index = GetFirstSelectedEnumIdIndex();

        return index != -1 ? _enumIds[index] : string.Empty;
    }

    /// <summary>
    /// Gets the (zero-based) index of the first EnumID that has a value of true.
    /// </summary>
    /// <returns>The index of the first EnumID if any enumerated values are set; -1 otherwise.</returns>
    public int GetFirstSelectedEnumIdIndex()
    {
        for (int n = 0; n < _enumStates.Length; n++)
        {
            if (_enumStates[n])
            {
                return n;
            }
        }

        return -1;
    }

    /// <summary>
    /// Gets the (zero-based) index of the supplied EnumID.
    /// </summary>
    /// <returns>The index of the supplied EnumID if that matches a valid enumerated value identifier; -1 otherwise.</returns>
    public int GetIndexOfEnumId(string enumId)
    {
        for (int n = 0; n < _enumIds.Length; n++)
        {
            if (_enumIds[n] == enumId)
            {
                return n;
            }
        }

        return -1;
    }

    /// <summary>
    /// Sets the state of all enumerated values to false.
    /// </summary>
    public void ClearAll()
    {
        _enumStates.SetAll(false);

        _nonEnumValue = null;
    }

    /// <summary>
    /// Loads the state of this EnumState from the supplied string in MultipleStringValue format, e.g., "EnumId_1 EnumId_2 EnumId_3".
    /// (Elements may be separated with space, semi-colon or comma.)
    /// </summary>
    /// <param name="initValues">MultipleStringValue format string containing the EnumIDs that specify the initial state for this EnumState.</param>
    /// <param name="allowNonEnumValue">If set to true, and initValues contains one or more values that don't correspond to a valid EnumID,
    /// then assume that initValues should be treated as the non-enumerated value for this EnumState.</param>
    /// <remarks>This method should not be confused with <see cref="FromWireValue"/>, as that method parses the supplied string for FIX wire
    /// values whereas this method parses the string for EnumIDs.</remarks>
    public void LoadInitValue(string initValues, bool allowNonEnumValue)
    {
        string[] enumIds = initValues.Split([';', ' ', ','], StringSplitOptions.RemoveEmptyEntries);

        bool allAreValid = true;

        // Verify that all EnumIds supplied in initValues are valid
        foreach (string enumId in enumIds)
        {
            allAreValid &= IsValidEnumId(enumId);
        }

        // Validate before mutating: if some EnumIDs are invalid and a non-enum value is not allowed,
        // throw before ClearAll so the current state is not left half-cleared / half-set (Theme D).
        if (!allAreValid && !allowNonEnumValue)
        {
            throw ThrowHelper.New<ArgumentException>(this, ErrorMessages.UnrecognisedEnumIdValue, initValues);
        }

        ClearAll();

        if (!allAreValid)
        {
            // allowNonEnumValue is necessarily true here.
            _nonEnumValue = initValues;
        }
        else
        {
            foreach (string enumId in enumIds)
            {
                this[enumId] = true;
            }
        }
    }

    /// <summary>
    /// Provides the state of this EnumState in a format ready to be sent over FIX, e.g., "A B C E".
    /// </summary>
    /// <param name="enumPairs">The EnumPairs for this parameter, to provide the mapping from EnumID values.</param>
    /// <returns>A space-separated string containing zero or more EnumPair WireValues.  If no EnumIDs are enabled,
    /// then null is returned.</returns>
    public string ToWireValue(EnumPairCollection enumPairs)
    {
        if (enumPairs.Count != _enumStates.Count)
        {
            throw ThrowHelper.New<InvalidOperationException>(ExceptionContext, ErrorMessages.InconsistentEnumPairsListItemsError);
        }

        // Override the values in the states collection if a non-enum value is supplied.  This is used to handle 
        // the unique case of the EditableDropDownList_t control.
        if (NonEnumValue != null)
        {
            return NonEnumValue.Length > 0 ? NonEnumValue : null!;
        }

        bool hasAtLeastOneValue = false;
        StringBuilder sb = new();

        for (int n = 0; n < _enumStates.Length; n++)
        {
            if (_enumStates[n])
            {
                string value = enumPairs.GetWireValueFromEnumId(_enumIds[n]);

                // Typically {NULL} will only be used in a mutually exclusive fashion, although nothing enforces this
                if (value != Atdl.NullValue)
                {
                    // Only prepend a space after the first entry
                    if (hasAtLeastOneValue)
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, " {0}", value);
                    }
                    else
                    {
                        sb.Append(value);

                        hasAtLeastOneValue = true;
                    }
                }
            }
        }

        return hasAtLeastOneValue ? sb.ToString() : null!;
    }

    /// <summary>
    /// Creates a new EnumState from the supplied set of EnumPairs and input FIX string.
    /// </summary>
    /// <param name="enumPairs">EnumPairs for this parameter.</param>
    /// <param name="multiValueString">String containing one or more FIX wire values (space-separated).</param>
    /// <returns></returns>
    public static EnumState FromWireValue(EnumPairCollection enumPairs, string multiValueString)
    {
        // Drop empty tokens so a blank, double-delimited or trailing-delimiter input does not yield
        // a "" token that TryParseWireValue would reject.
        string[] inputValues = multiValueString.Split([';', ' ', ','], StringSplitOptions.RemoveEmptyEntries);

        EnumState result = new(enumPairs.EnumIds);

        foreach (string inputValue in inputValues)
        {

            if (!enumPairs.TryParseWireValue(inputValue, out string? enumId))
            {
                throw ThrowHelper.New<ArgumentException>(ExceptionContext, ErrorMessages.UnrecognisedEnumIdValue, inputValue);
            }

            result[enumId!] = true;
        }

        return result;
    }

    /// <summary>
    /// Provides a string representation of this EnumState.  Primarily for debugging purposes.
    /// </summary>
    /// <returns>String representation of this EnumState.</returns>
    public override string ToString()
    {
        StringBuilder sb = new();

        sb.Append('(');

        for (int n = 0; n < _enumStates.Length; n++)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}{2}", _enumIds[n], _enumStates[n].ToString().ToLowerInvariant(), n < _enumStates.Length - 1 ? ", " : string.Empty);
        }

        if (_nonEnumValue != null)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, ", NonEnumValue='{0}'", _nonEnumValue);
        }

        sb.Append(')');

        return sb.ToString();
    }

}
