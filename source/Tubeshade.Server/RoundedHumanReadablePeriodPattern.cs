using System;
using System.Globalization;
using System.Text;
using NodaTime;
using NodaTime.Text;

namespace Tubeshade.Server;

public sealed class RoundedHumanReadablePeriodPattern : IPattern<Period>
{
    public const int MaximumPlaces = 4;

    public static readonly RoundedHumanReadablePeriodPattern TwoPlaces = new(2);

    private readonly int _places;

    public RoundedHumanReadablePeriodPattern(int places)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(places);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(places, MaximumPlaces);

        _places = places;
    }

    /// <inheritdoc />
    public ParseResult<Period> Parse(string text) => throw new NotSupportedException();

    /// <inheritdoc />
    public string Format(Period value) => AppendFormat(value, new StringBuilder()).ToString();

    /// <inheritdoc />
    public StringBuilder AppendFormat(Period value, StringBuilder builder)
    {
        value = value.Normalize();
        if (value.Equals(Period.Zero))
        {
            return builder;
        }

        Span<PeriodValue> values = stackalloc[]
        {
            new PeriodValue('d', long.MaxValue, value.Days),
            new PeriodValue('h', 24, value.Hours),
            new PeriodValue('m', 60, value.Minutes),
            new PeriodValue('s', 60, value.Seconds),
        };

        var firstIndex = 0;
        for (; firstIndex < values.Length; firstIndex++)
        {
            if (values[firstIndex].Value is not 0)
            {
                break;
            }
        }

        if (values.Length - firstIndex <= _places)
        {
            var length = values.Length;
            while (values[length - 1].Value is 0)
            {
                length--;
            }

            for (var i = firstIndex; i < length; i++)
            {
                var current = values[i];
                AppendValue(builder, current.Value, current.Suffix, i == length - 1);
            }

            return builder;
        }

        for (var i = _places - 1; i >= 0; i--)
        {
            var index = firstIndex + i;
            var isLast = i + 1 == _places;

            if (isLast)
            {
                var next = values[index + 1];
                if (next.Value >= next.Limit / 2)
                {
                    values[index].Value += 1;
                }
            }

            var current = values[index];
            if (current.Value >= current.Limit)
            {
                values[index].Value = 0;
                values[index - 1].Value += 1;
            }
        }

        firstIndex = 0;
        for (; firstIndex < values.Length; firstIndex++)
        {
            if (values[firstIndex].Value is not 0)
            {
                break;
            }
        }

        var count = _places;
        while (values[count + firstIndex - 1].Value is 0)
        {
            count--;
        }

        for (var i = 0; i < count; i++)
        {
            var index = firstIndex + i;
            var current = values[index];
            var isLast = i + 1 == count;
            AppendValue(builder, current.Value, current.Suffix, isLast);
        }

        return builder;
    }

    private static void AppendValue(StringBuilder builder, long value, char suffix, bool last = false)
    {
        if (value is 0)
        {
            return;
        }

        builder.Append(value.ToString("D", CultureInfo.InvariantCulture));
        builder.Append(suffix);

        if (!last)
        {
            builder.Append(' ');
        }
    }

    private struct PeriodValue
    {
        public readonly char Suffix;
        public readonly long Limit;
        public long Value;

        public PeriodValue(char suffix, long limit, long value)
        {
            Suffix = suffix;
            Limit = limit;
            Value = value;
        }
    }
}
