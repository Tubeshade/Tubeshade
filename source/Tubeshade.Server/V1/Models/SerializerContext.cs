using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tubeshade.Server.V1.Models;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(CookieUpdateRequest))]
[JsonSerializable(typeof(Library))]
public sealed partial class SerializerContext : JsonSerializerContext;
