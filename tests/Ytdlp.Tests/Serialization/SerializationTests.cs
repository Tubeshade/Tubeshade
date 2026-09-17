using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using VerifyNUnit;
using YoutubeDLSharp.Metadata;

namespace Ytdlp.Tests.Serialization;

public sealed class SerializationTests
{
    [TestCase("PLzH6n4zXuckqbjBDsA_Hc-q5hI2lGml1F.json", TestName = "Playlist")]
    public async Task VideoData(string fileName)
    {
        await using var fileStream = File.OpenRead(GetRelativePath(fileName));
        var data = (await JsonSerializer.DeserializeAsync(fileStream, YouTubeSerializerContext.Default.VideoData))!;
        data.Should().NotBeNull();

        using var scope = new AssertionScope();

        await Verifier.Verify(data);
    }

    private static string GetRelativePath(string fileName, [CallerFilePath] string path = "")
    {
        var directory = Path.GetDirectoryName(path)!;
        return Path.Combine(directory, fileName);
    }
}
