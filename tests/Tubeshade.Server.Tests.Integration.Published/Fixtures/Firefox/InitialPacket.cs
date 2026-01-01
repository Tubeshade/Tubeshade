using System.Collections.Generic;

namespace Tubeshade.Server.Tests.Integration.Published.Fixtures.Firefox;

public sealed class InitialPacket : ResponsePacket
{
    public required string ApplicationType { get; init; }

    public Dictionary<string, object>? Traits { get; init; }
}
