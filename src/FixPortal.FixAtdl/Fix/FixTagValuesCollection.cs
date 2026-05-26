// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Collections.Generic;
using FixPortal.FixAtdl.Utility;

namespace FixPortal.FixAtdl.Fix;

public class FixTagValuesCollection : IEnumerable<KeyValuePair<FixField, string>>
{
    private readonly FixMessage _message;

    public FixTagValuesCollection()
    {
        _message = [];
    }

    public FixTagValuesCollection(string fixMessage)
        : this(new FixMessage(fixMessage))
    {
    }

    public FixTagValuesCollection(FixMessage message)
    {
        _message = message;
    }

    public static FixTagValuesCollection Empty { get; } = [];

    public string this[FixField fixField]
    {
        get => _message[fixField]; set => _message[fixField] = value;
    }

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

    public bool TryGetValue(string fixField, out string value)
    {
        FixField field = fixField.ParseAsEnum<FixField>();

        bool result = _message.TryGetValue(field, out string? v);
        value = v!;
        return result;
    }

    public bool TryGetValue(FixTag tag, out string value)
    {
        FixField field = tag;

        bool result = _message.TryGetValue(field, out string? v);
        value = v!;
        return result;
    }

    public void Add(FixTag tag, string value)
    {
        _message.Add(tag, value);
    }

    public string ToFix()
    {
        return _message.ToFix();
    }

    public override string ToString()
    {
        return ToFix().Replace("\x01", " | ");
    }

    public IEnumerator<KeyValuePair<FixField, string>> GetEnumerator()
    {
        return _message.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return _message.GetEnumerator();
    }
}

