using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Tubeshade.Server.Tests.Integration.Published.Fixtures.Firefox;

public sealed class FirefoxDebuggerClient : IDisposable
{
    private const char Separator = ':';

    private readonly int _port;
    private readonly TcpClient _tcpClient;

    public FirefoxDebuggerClient(int port)
    {
        _port = port;
        _tcpClient = new TcpClient();
    }

    public async Task Connect(CancellationToken cancellationToken = default)
    {
        await _tcpClient.ConnectAsync(new IPEndPoint(IPAddress.Loopback, _port), cancellationToken);

        var initial = await ReadPacket<InitialPacket>(FirefoxContext.Default.InitialPacket, cancellationToken);
        if (initial is not { From: "root", ApplicationType: "browser" })
        {
            throw new InvalidOperationException("Unexpected first packet");
        }
    }

    public async Task InstallExtension(string path,
        CancellationToken cancellationToken = default)
    {
        await SendPacket(
            new RequestPacket { To = "root", Type = "getRoot", },
            FirefoxContext.Default.RequestPacket,
            cancellationToken);

        var getRoot = await ReadPacket<GetRootPacket>(FirefoxContext.Default.GetRootPacket, cancellationToken);
        if (getRoot.AddonsActor is not { } addonsActor)
        {
            throw new InvalidOperationException("Could not determine addons actor");
        }

        await SendPacket(
            new InstallTemporaryAddonRequest { To = addonsActor, Type = "installTemporaryAddon", AddonPath = path },
            FirefoxContext.Default.InstallTemporaryAddonRequest,
            cancellationToken);

        await ReadPacket<InstallTemporaryAddonResponse>(
            FirefoxContext.Default.InstallTemporaryAddonResponse,
            cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose() => _tcpClient.Dispose();

    private async ValueTask<TPacket> ReadPacket<TPacket>(
        JsonTypeInfo<TPacket> typeInfo,
        CancellationToken cancellationToken)
    {
        Span<byte> lengthBuffer = stackalloc byte[1024];
        var stream = _tcpClient.GetStream();
        var length = -1;

        for (var index = 0; index < lengthBuffer.Length; index++)
        {
            var current = stream.ReadByte();
            if (current is -1)
            {
                break;
            }

            if (current is Separator)
            {
                length = int.Parse(lengthBuffer[..index]);
                break;
            }

            lengthBuffer[index] = (byte)current;
        }

        if (length < 1)
        {
            throw new InvalidOperationException("Failed to read packet length");
        }

        var jsonBuffer = new byte[length];
        await stream.ReadExactlyAsync(jsonBuffer, cancellationToken);

        return JsonSerializer.Deserialize(jsonBuffer, typeInfo)!;
    }

    private async ValueTask SendPacket<TPacket>(
        TPacket packet,
        JsonTypeInfo<TPacket> typeInfo,
        CancellationToken cancellationToken)
        where TPacket : RequestPacket
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(packet, typeInfo);

        var stream = _tcpClient.GetStream();
        var length = Encoding.UTF8.GetBytes($"{bytes.Length.ToString()}:");

        await stream.WriteAsync(length, cancellationToken);
        await stream.WriteAsync(bytes, cancellationToken);
    }
}
