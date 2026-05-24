#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;

namespace FixPortal.FixAtdl.Xml.Serialization;

public class ClassDeserializedEventArgs(Type createdType, object extraInfo) : EventArgs
{
    public Type ClassType { get; private set; } = createdType;
    public object ExtraInfo { get; private set; } = extraInfo;
}

