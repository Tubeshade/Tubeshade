using System.Text.Json;
using Tubeshade.Server.V1.Models;

namespace Tubeshade.Server.Pages.Shared;

public static class StringExtensions
{
    public static string ToJson(this string text) => JsonSerializer.Serialize(text, SerializerContext.Default.String);
}
