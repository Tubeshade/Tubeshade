namespace Tubeshade.Server.Tests.Integration.Published.Fixtures.Firefox;

public sealed class InstallTemporaryAddonResponse : ResponsePacket
{
    public required FirefoxAddonActor Addon { get; init; }
}
