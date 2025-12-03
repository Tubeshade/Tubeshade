using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Tubeshade.Server.Tests.Services.Ffmpeg;

internal static class StringExtensions
{
    internal static string GetRelativePath(this string fileName, [CallerFilePath] string path = "")
    {
        var directory = Path.GetDirectoryName(path);
        return string.IsNullOrWhiteSpace(directory)
            ? throw new InvalidOperationException($"Could not find directory for file {path}")
            : Path.Combine(directory, fileName);
    }
}
