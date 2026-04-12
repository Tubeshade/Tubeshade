using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.SmartEnum;

namespace Tubeshade.Data.Media;

public sealed class HashAlgorithm : SmartEnum<HashAlgorithm, int>
{
    public static readonly HashAlgorithm Placeholder = new(Names.Placeholder, 1, PlaceholderHash);
    public static readonly HashAlgorithm Sha256 = new(Names.Sha256, 2, SHA256.Create);

    public static HashAlgorithm Default => Sha256;

    private static readonly FileStreamOptions FileStreamOptions = new()
    {
        Access = FileAccess.Read,
        Mode = FileMode.Open,
        Options = FileOptions.SequentialScan,
        Share = FileShare.Read,
    };

    private readonly Func<System.Security.Cryptography.HashAlgorithm> _algorithmFactory;

    private HashAlgorithm(string name, int value, Func<System.Security.Cryptography.HashAlgorithm> algorithmFactory)
        : base(name, value)
    {
        _algorithmFactory = algorithmFactory;
    }

    public async ValueTask<byte[]> ComputeHashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        using var algorithm = _algorithmFactory();
        await using var stream = File.Open(filePath, FileStreamOptions);
        return await algorithm.ComputeHashAsync(stream, cancellationToken);
    }

    public async ValueTask<byte[]> ComputeHashAsync(FileInfo file, CancellationToken cancellationToken = default)
    {
        using var algorithm = _algorithmFactory();
        await using var stream = file.Open(FileStreamOptions);
        return await algorithm.ComputeHashAsync(stream, cancellationToken);
    }

    public async ValueTask<byte[]> ComputeHashAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var algorithm = _algorithmFactory();
        return await algorithm.ComputeHashAsync(stream, cancellationToken);
    }

    [DoesNotReturn]
    private static System.Security.Cryptography.HashAlgorithm PlaceholderHash()
    {
        throw new InvalidOperationException($"{Names.Placeholder} cannot be used for hashing");
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string Placeholder = "placeholder";
        public const string Sha256 = "SHA256";
    }
}
