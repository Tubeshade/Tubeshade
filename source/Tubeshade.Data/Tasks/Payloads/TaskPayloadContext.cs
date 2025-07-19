using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tubeshade.Data.Tasks.Payloads;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(IndexPayload))]
[JsonSerializable(typeof(DownloadVideoPayload))]
[JsonSerializable(typeof(ScanChannelPayload))]
public sealed partial class TaskPayloadContext : JsonSerializerContext;
