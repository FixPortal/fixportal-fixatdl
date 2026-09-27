// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
// FP Enhancement: 2026-09-27 — value/wire/conversion machinery moved to the shared generic base
// AtdlParameterTypeBase<TStorage>; this class remains as the value-type pivot that pins storage to T?
// (Nullable<T>). Behaviour is unchanged.
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Model.Types.Support;

/// <summary>
/// Base class for all value type parameters (Int_t, Float_t, etc.).
/// </summary>
/// <remarks>Parameter types must be one of <see cref="AtdlValueType{T}"/> or <see cref="AtdlReferenceType{T}"/>.
/// The reason for the differentiation is that most FIXatdl types that use value types for the underlying storage
/// (Int_t, Float_t, UTCTimestamp_t, etc.) actually use <see cref="Nullable{T}"/> so that they can also contain
/// null, meaning don't include this value in the FIX output.  However, Nullable&lt;T&gt; is a value type, not
/// a reference type, and so a different pivot is required to support underlying reference type usage, such
/// as in String_t.  The two pivots share their implementation through
/// <see cref="AtdlParameterTypeBase{TStorage}"/>: this class pins the storage type to T? (i.e.
/// Nullable&lt;T&gt;), while <see cref="AtdlReferenceType{T}"/> pins it to T itself.  (Factoring the shared
/// code out used to be impossible because the pivots disagree on what T? means; the unconstrained
/// TStorage? annotation on the shared base now carries exactly the right storage type for each.)</remarks>
public abstract class AtdlValueType<T> : AtdlParameterTypeBase<T?>
    where T : struct { }
