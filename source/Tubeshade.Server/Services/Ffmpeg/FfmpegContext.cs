using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tubeshade.Server.Services.Ffmpeg;

[JsonSourceGenerationOptions(JsonSerializerDefaults.General, NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(ProbeResponse))]
public sealed partial class FfmpegContext : JsonSerializerContext;
