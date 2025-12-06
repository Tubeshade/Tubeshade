using System.Text.Json;
using System.Text.Json.Serialization;

namespace YoutubeDLSharp.Metadata;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(VideoData))]
public sealed partial class YouTubeSerializerContext : JsonSerializerContext;
