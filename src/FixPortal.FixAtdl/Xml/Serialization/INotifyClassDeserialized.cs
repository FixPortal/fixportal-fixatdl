// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;

namespace FixPortal.FixAtdl.Xml.Serialization;

/// <summary>
/// Exposes notifications when a configured type has been deserialized.
/// </summary>
public interface INotifyClassDeserialized
{
    /// <summary>
    /// Occurs when a class has been deserialized.
    /// </summary>
    event EventHandler<ClassDeserializedEventArgs> ClassDeserialized;
}
