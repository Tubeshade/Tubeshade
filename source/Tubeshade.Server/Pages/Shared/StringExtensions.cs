using System;
using System.Collections.Generic;
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

        public List<string> GetLines()
        {
            var lines = new List<string>();

            foreach (var range in LineSplit().EnumerateSplits(text))
            {
                lines.Add(text[range]);
            }

            return lines;
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

        public List<string> GetNonEmptyLines()
        {
            var lines = new List<string>();
            var span = text.AsSpan();

            foreach (var range in LineSplitTrimmed().EnumerateSplits(span))
            {
                var lineSpan = span[range];
                if (lineSpan.IsEmpty || lineSpan.IsWhiteSpace())
                {
                    continue;
                }

                lines.Add(lineSpan.Trim().ToString());
            }

            return lines;
        }

        public int GetNonEmptyLineCount()
        {
            var count = 0;
            var span = text.AsSpan();

            foreach (var range in LineSplitTrimmed().EnumerateSplits(span))
            {
                var lineSpan = span[range];
                if (lineSpan.IsEmpty || lineSpan.IsWhiteSpace())
                {
                    continue;
                }

                count++;
            }

            return count;
        }
    }

    [GeneratedRegex(@"(?:\r?\n){1}", RegexOptions.None, 100)]
    public static partial Regex LineSplit();

    [GeneratedRegex(@"\s*(?:\r?\n){1}\s*", RegexOptions.None, 100)]
    public static partial Regex LineSplitTrimmed();

    [GeneratedRegex(@"\s*(?:\r?\n){2,}\s*", RegexOptions.None, 100)]
    public static partial Regex ParagraphSplit();
}
