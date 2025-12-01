using System.Text.Json;
using Tubeshade.Server.V1.Models;
using static System.StringSplitOptions;

namespace Tubeshade.Server.Pages.Shared;

public static class StringExtensions
{
    private static readonly string[] ParagraphDelimiters = ["\r\n\r\n", "\n\n"];

    public static string ToJson(this string text)
    {
        return JsonSerializer.Serialize(text, SerializerContext.Default.String);
    }

    public static string[] ToParagraphs(this string text)
    {
        return text.Split(ParagraphDelimiters, RemoveEmptyEntries | TrimEntries);
    }
}
