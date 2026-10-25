using System.IO;
using NodaTime;
using Tubeshade.Data.Preferences;
using YoutubeDLSharp.Metadata;

namespace Tubeshade.Server.Services;

internal sealed class DataFile
{
    public DataFile(string path, PlayerClient? playerClient, VideoData data)
    {
        Path = path;
        PlayerClient = playerClient;
        Data = data;
    }

    public string Path { get; }

    public PlayerClient? PlayerClient { get; private set; }

    public VideoData Data { get; private set; }

    public Instant? FetchedAt => Data.Epoch is { } epoch ? Instant.FromUnixTimeSeconds(epoch) : null;

    public static string GetPath(DirectoryInfo directory) => System.IO.Path.Combine(directory.FullName, "info.json");

    public void Update(PlayerClient? playerClient, VideoData data)
    {
        PlayerClient = playerClient;
        Data = data;
    }
}
