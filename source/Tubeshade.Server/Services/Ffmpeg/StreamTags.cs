using System.Text.Json.Serialization;

namespace Tubeshade.Server.Services.Ffmpeg;

public sealed class StreamTags
{
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("handler_name")]
    public string? HandlerName { get; set; }

    [JsonPropertyName("vendor_id")]
    public string? VendorId { get; set; }

    [JsonPropertyName("duration")]
    public string? Duration { get; set; }
}
