using System;
using System.Globalization;
using System.Text;
using NodaTime;
using NodaTime.Text;

namespace Tubeshade.Server;

public sealed class HumanReadablePeriodPattern : IPattern<Period>
{
    /// <inheritdoc />
    public ParseResult<Period> Parse(string text)
    {
        throw new NotSupportedException("Cannot parse human readable periods");
    }

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

        AppendValue(builder, value.Days, 'd');
        AppendValue(builder, value.Hours, 'h');
        AppendValue(builder, value.Minutes, 'm');
        AppendValue(builder, value.Seconds, 's', true);

        return builder;
    }

    private static void AppendValue(StringBuilder builder, long value, char suffix, bool last = false)
    {
        if (value == 0)
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
}
