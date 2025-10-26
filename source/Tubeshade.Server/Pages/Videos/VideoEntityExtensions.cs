using System;
using System.IO;
using Tubeshade.Data.Media;

namespace Tubeshade.Server.Pages.Videos;

public static class VideoEntityExtensions
{
    public static string GetSubtitlesFilePath(this VideoEntity video, string language = "en")
    {
        return video.GetFilePath($"subtitles.{language}.vtt");
    }

    public static string GetChaptersFilePath(this VideoEntity video)
    {
        return video.GetFilePath("chapters.vtt");
    }

    public static string GetThumbnailFilePath(this VideoEntity video)
    {
        return video.GetFilePath("thumbnail.jpg");
    }

    public static string GetDirectoryPath(this VideoEntity video)
    {
        var attributes = File.GetAttributes(video.StoragePath);
        var targetDirectory = (attributes & FileAttributes.Directory) is FileAttributes.Directory
            ? video.StoragePath
            : Path.GetDirectoryName(video.StoragePath);

        if (targetDirectory is null)
        {
            throw new InvalidOperationException("Video storage path does not contain a valid directory");
        }

        return targetDirectory;
    }

    private static string GetFilePath(this VideoEntity video, string fileName)
    {
        var attributes = File.GetAttributes(video.StoragePath);
        return (attributes & FileAttributes.Directory) is FileAttributes.Directory
            ? Path.Combine(video.StoragePath, fileName)
            : Path.Combine(Path.GetDirectoryName(video.StoragePath) ?? string.Empty, fileName);
    }
}
