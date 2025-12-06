using System;
using System.Collections.Generic;
using System.Linq;

namespace YoutubeDLSharp.Options;

public sealed class MultiValue<T>
{
    public List<T> Values { get; }

    public MultiValue(params T[] values)
    {
        Values = values.ToList();
    }

    public static implicit operator MultiValue<T>(T value) => new(value);

    public static implicit operator MultiValue<T>(T[] values) => new(values);

    public static explicit operator T(MultiValue<T> value)
    {
        if (value.Values is [var singleValue])
        {
            return singleValue;
        }

        throw new InvalidCastException($"Cannot cast sequence of values to {typeof(T)}.");
    }

    public static explicit operator T[](MultiValue<T> value) => value.Values.ToArray();
}
