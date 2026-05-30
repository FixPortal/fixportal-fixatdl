// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Xml.Linq;

namespace FixPortal.FixAtdl.Xml;

/// <summary>
/// Provides the XML namespaces used by FIXatdl documents.
/// </summary>
public static class AtdlNamespaces
{
    /// <summary>
    /// The FIXatdl core namespace.
    /// </summary>
    public static readonly XNamespace core = "http://www.fixprotocol.org/FIXatdl-1-1/Core";

    /// <summary>
    /// The FIXatdl layout namespace.
    /// </summary>
    public static readonly XNamespace lay = "http://www.fixprotocol.org/FIXatdl-1-1/Layout";

    /// <summary>
    /// The FIXatdl validation namespace.
    /// </summary>
    public static readonly XNamespace val = "http://www.fixprotocol.org/FIXatdl-1-1/Validation";

    /// <summary>
    /// The FIXatdl flow namespace.
    /// </summary>
    public static readonly XNamespace flow = "http://www.fixprotocol.org/FIXatdl-1-1/Flow";

    /// <summary>
    /// The XML Schema Instance namespace.
    /// </summary>
    public static readonly XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
}
