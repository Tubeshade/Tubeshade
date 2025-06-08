using System.Diagnostics.Contracts;
using System.Text;
using System.Text.RegularExpressions;
using static System.Text.RegularExpressions.RegexOptions;

namespace Tubeshade.Data;

public static partial class StringExtensions
{
    [GeneratedRegex(@"\s+", CultureInvariant | IgnoreCase, 100)]
    private static partial Regex WhitespaceRegex();

    [Pure]
    public static string NormalizeInvariant(this string value, bool whitespace = true)
    {
        // todo: consider replacing lookalike characters
        value = value.ToUpperInvariant();

        // todo: hack
        if (whitespace)
        {
            value = WhitespaceRegex().Replace(value, string.Empty);
        }

        value = value.Normalize(NormalizationForm.FormC);

        return value;
    }
}
