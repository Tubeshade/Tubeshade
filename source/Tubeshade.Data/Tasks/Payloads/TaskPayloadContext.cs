using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tubeshade.Data.Tasks.Payloads;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(IndexVideoPayload))]
[JsonSerializable(typeof(DownloadVideoPayload))]
public sealed partial class TaskPayloadContext : JsonSerializerContext;
