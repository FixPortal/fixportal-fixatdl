// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Diagnostics;
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Resources;

namespace FixPortal.FixAtdl.Utility;

/// <summary>
/// Provides helper methods for working with <see cref="Process"/> instances.
/// </summary>
public static class ProcessExtensions
{
    private static readonly string ExceptionContext = "FixPortal.FixAtdl.Utility.ProcessExtensions";

    /// <summary>
    /// Determines whether the specified process is a Visual Studio designer process.
    /// </summary>
    /// <param name="process">The process to inspect.</param>
    /// <returns><see langword="true"/> if the process appears to be Visual Studio; otherwise, <see langword="false"/>.</returns>
    public static bool IsVSDesigner(this Process process)
    {
        if (process == null)
        {
            // Bad input is an argument error, not a manufactured NullReferenceException (F5).
            throw ThrowHelper.NewWithParamName<ArgumentNullException>(ExceptionContext, nameof(process), ErrorMessages.IllegalUseOfNullError);
        }

        try
        {
            return process.MainModule?.ModuleName.Contains("devenv.exe") ?? false;
        }
        catch (Exception)
        {
            // Reading MainModule can itself throw (process exited, access denied, cross-bitness). A
            // boolean probe must not raise framework exceptions of its own (G-E).
            return false;
        }
    }
}
