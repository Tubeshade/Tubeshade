using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using NodaTime;
using NodaTime.Text;

namespace Tubeshade.Server;

public sealed partial class HumanReadablePeriodPattern : IPattern<Period>
{
    public static readonly HumanReadablePeriodPattern Instance = new();

    /// <inheritdoc />
    public ParseResult<Period> Parse(string text)
    {
        var match = ParseRegex().Match(text);
        if (!match.Success ||
            match.Groups is [_, { Length: <= 1 }, { Length: <= 1 }, { Length: <= 1 }, { Length: <= 1 }])
        {
            return ParseResult<Period>.ForException(() => new FormatException("Could not parse period"));
        }

        var builder = new PeriodBuilder();

        try
        {
            if (match.Groups[1].ValueSpan is { Length: > 1 } days)
            {
                builder.Days = int.Parse(days[..^1], CultureInfo.InvariantCulture);
            }

            if (match.Groups[2].ValueSpan is { Length: > 1 } hours)
            {
                builder.Hours = int.Parse(hours[..^1], CultureInfo.InvariantCulture);
            }

            if (match.Groups[3].ValueSpan is { Length: > 1 } minutes)
            {
                builder.Minutes = int.Parse(minutes[..^1], CultureInfo.InvariantCulture);
            }

            if (match.Groups[4].ValueSpan is { Length: > 1 } seconds)
            {
                builder.Seconds = int.Parse(seconds[..^1], CultureInfo.InvariantCulture);
            }
        }
        catch (Exception exception)
        {
            return ParseResult<Period>.ForException(() => exception);
        }


        return ParseResult<Period>.ForValue(builder.Build());
    }

    [GeneratedRegex(@"^\s*(\d{1,}d)?\s*(\d{1,}h)?\s*(\d{1,}m)?\s*(\d{1,}s)?\s*$")]
    private static partial Regex ParseRegex();

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
