// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;

namespace FixPortal.FixAtdl.Xml.Serialization;

/// <summary>
/// Provides data for notifications raised when a class has been deserialized.
/// </summary>
/// <param name="createdType">The type that was created.</param>
/// <param name="extraInfo">Additional information about the created object.</param>
public class ClassDeserializedEventArgs(Type createdType, object extraInfo) : EventArgs
{
    /// <summary>
    /// Gets the type that was deserialized.
    /// </summary>
    public Type ClassType { get; private set; } = createdType;

    /// <summary>
    /// Gets additional information about the deserialized object.
    /// </summary>
    public object ExtraInfo { get; private set; } = extraInfo;
}
