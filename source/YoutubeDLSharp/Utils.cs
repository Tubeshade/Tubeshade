using System;
using System.IO;

namespace YoutubeDLSharp;

/// <summary>
/// Utility methods.
/// </summary>
public static class Utils
{
    /// <summary>
    /// Returns the absolute path for the specified path string.
    /// Also searches the environment's PATH variable.
    /// </summary>
    /// <param name="fileName">The relative path string.</param>
    /// <returns>The absolute path or null if the file was not found.</returns>
    public static string GetFullPath(string fileName)
    {
        if (File.Exists(fileName))
        {
            return Path.GetFullPath(fileName);
        }

        var values = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var p in values.Split(Path.PathSeparator))
        {
            var fullPath = Path.Combine(p, fileName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        throw new InvalidOperationException($"Failed to get full path for file {fileName}");
    }
}
