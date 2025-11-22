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
    public static readonly HashAlgorithm Placeholder = new(Names.Placeholder, 1, () => throw new InvalidOperationException($"{Names.Placeholder} cannot be used for hashing"));
    public static readonly HashAlgorithm Sha256 = new(Names.Sha256, 2, SHA256.Create);

    public static HashAlgorithm Default => Sha256;

    private readonly Func<System.Security.Cryptography.HashAlgorithm> _algorithmFactory;

    private System.Security.Cryptography.HashAlgorithm? _algorithm;

    private System.Security.Cryptography.HashAlgorithm Algorithm => _algorithm ??= _algorithmFactory();

    private HashAlgorithm(string name, int value, Func<System.Security.Cryptography.HashAlgorithm> algorithmFactory)
        : base(name, value)
    {
        _algorithmFactory = algorithmFactory;
    }

    public async ValueTask<byte[]> ComputeHashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var file = File.OpenRead(filePath);
        return await Algorithm.ComputeHashAsync(file, cancellationToken);
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string Placeholder = "placeholder";
        public const string Sha256 = "SHA256";
    }
}
