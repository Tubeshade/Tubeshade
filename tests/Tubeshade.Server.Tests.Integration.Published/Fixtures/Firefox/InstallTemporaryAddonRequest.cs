namespace Tubeshade.Server.Tests.Integration.Published.Fixtures.Firefox;

public sealed class InstallTemporaryAddonRequest : RequestPacket
{
    public required string AddonPath { get; init; }
}
