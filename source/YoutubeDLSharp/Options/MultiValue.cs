using System;
using System.Collections.Generic;

namespace YoutubeDLSharp.Options;

public sealed class MultiValue<T>
{
    public List<T> Values { get; }

    public MultiValue(params ReadOnlySpan<T> values)
    {
        Values = [];
        Values.AddRange(values);
    }

    public static implicit operator MultiValue<T>(T value) => new(value);
}
