// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
// FP Enhancement: 2026-09-27 — value/wire/conversion machinery moved to the shared generic base
// AtdlParameterTypeBase<TStorage>; this class remains as the reference-type pivot that pins storage to
// T itself. NormalizeControlCandidate maps an emptied string or char[] control candidate to null, so an
// emptied Data_t stores null rather than an empty value.
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Model.Types.Support;

/// <summary>
/// Base class for all reference type parameters (String_t, MultipleCharValue_t, MultipleStringValue_t, etc.).
/// </summary>
/// <remarks>Parameter types must be one of <see cref="AtdlValueType{T}"/> or <see cref="AtdlReferenceType{T}"/>.
/// The reason for the differentiation is that most FIXatdl types that use value types for the underlying storage
/// (Int_t, Float_t, UTCTimestamp_t, etc.) actually use <see cref="Nullable{T}"/> so that they can also contain
/// null, meaning don't include this value in the FIX output.  However, Nullable&lt;T&gt; is a value type, not
/// a reference type, and so a different pivot is required to support underlying reference type usage, such
/// as in String_t.  The two pivots share their implementation through
/// <see cref="AtdlParameterTypeBase{TStorage}"/>: <see cref="AtdlValueType{T}"/> pins the storage type to T?
/// (i.e. Nullable&lt;T&gt;), while this class pins it to T itself.</remarks>
public abstract class AtdlReferenceType<T> : AtdlParameterTypeBase<T>
    where T : class
{
    /// <summary>
    /// Normalises a control-supplied candidate before validation: an empty string or empty char array
    /// from a cleared reference-type control maps to null (the FIXatdl no-value state), rather than
    /// validating as a set-but-empty value.
    /// </summary>
    /// <param name="candidate">Value returned by ConvertToNativeType, may be null.</param>
    /// <returns>Null when the candidate is an empty string or empty char array; the candidate unchanged otherwise.</returns>
    /// <remarks>This is the one place the reference pivot's SetValueFromControl differed from the value
    /// pivot's.  <see cref="AtdlValueType{T}"/> has no equivalent: a Nullable&lt;T&gt; candidate cannot
    /// express "empty", so the base identity implementation stands there.</remarks>
    protected override T? NormalizeControlCandidate(T? candidate)
    {
        return candidate is string { Length: 0 } or char[] { Length: 0 } ? null : candidate;
    }
}
