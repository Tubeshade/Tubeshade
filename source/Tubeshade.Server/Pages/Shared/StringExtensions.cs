using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tubeshade.Server.V1.Models;

namespace Tubeshade.Server.Pages.Shared;

public static partial class StringExtensions
{
    extension(string text)
    {
        public string ToJson()
        {
            return JsonSerializer.Serialize(text, SerializerContext.Default.String);
        }

        public string? GetFirstParagraph()
        {
            var span = text.AsSpan();

            foreach (var splitRange in ParagraphSplit().EnumerateSplits(span))
            {
                var splitSpan = span[splitRange];
                if (splitSpan.IsEmpty || splitSpan.IsWhiteSpace())
                {
                    continue;
                }

                return splitSpan.ToString();
            }

            return null;
        }

        public int GetLineCount()
        {
            var count = 0;

            foreach (var _ in LineSplit().EnumerateSplits(text))
            {
                count++;
            }

            return count;
        }
    }

    [GeneratedRegex(@"(?:\r?\n){1}", RegexOptions.None, 100)]
    public static partial Regex LineSplit();

    [GeneratedRegex(@"\s*(?:\r?\n){2,}\s*", RegexOptions.None, 100)]
    public static partial Regex ParagraphSplit();
}
