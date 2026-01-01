using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tubeshade.Server.Tests.Integration.Published.Fixtures.Firefox;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(InitialPacket))]
[JsonSerializable(typeof(RequestPacket))]
[JsonSerializable(typeof(GetRootPacket))]
[JsonSerializable(typeof(InstallTemporaryAddonRequest))]
[JsonSerializable(typeof(InstallTemporaryAddonResponse))]
public sealed partial class FirefoxContext : JsonSerializerContext;
