// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Model.Types.Support;

namespace FixPortal.FixAtdl.Model.Types;

/// <summary>
/// 'int field representing the length in bytes. Value must be positive.'
/// </summary>
/// <remarks>The FIX contract requires a positive (non-zero) value, hence the non-zero base.</remarks>
public class Length_t : NonZeroPositiveIntegerTypeBase;

