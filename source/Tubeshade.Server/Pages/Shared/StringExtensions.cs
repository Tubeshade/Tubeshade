using System.Text.Json;
using Tubeshade.Server.V1.Models;
using static System.StringSplitOptions;

namespace Tubeshade.Server.Pages.Shared;

public static class StringExtensions
{
    private static readonly string[] LineDelimiters = ["\r\n", "\n"];
    private static readonly string[] ParagraphDelimiters = ["\r\n\r\n", "\n\n"];

    extension(string text)
    {
        public string ToJson()
        {
            return JsonSerializer.Serialize(text, SerializerContext.Default.String);
        }

        public string[] ToParagraphs()
        {
            return text.Split(ParagraphDelimiters, RemoveEmptyEntries | TrimEntries);
        }

        public string[] ToLines()
        {
            return text.Split(LineDelimiters, None);
        }
    }
}
