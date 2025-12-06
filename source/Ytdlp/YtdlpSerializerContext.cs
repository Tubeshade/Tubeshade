using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ytdlp;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(VideoData))]
public sealed partial class YtdlpSerializerContext : JsonSerializerContext;
