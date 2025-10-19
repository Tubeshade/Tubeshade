using System.Text.Json.Serialization;

namespace Tubeshade.Server.Services.Ffmpeg;

public sealed class ProbeResponse
{
    [JsonPropertyName("streams")]
    public Stream[]? Streams { get; set; }

    [JsonPropertyName("format")]
    public Format? Format { get; set; }
}
