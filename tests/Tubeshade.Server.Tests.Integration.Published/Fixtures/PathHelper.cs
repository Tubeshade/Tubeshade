using System.IO;
using System.Runtime.CompilerServices;

namespace Tubeshade.Server.Tests.Integration.Published.Fixtures;

internal static class PathHelper
{
    internal static string GetRelativePath(string relativePath, [CallerFilePath] string filePath = "")
    {
        var directory = Path.GetDirectoryName(filePath)!;
        return Path.GetFullPath(relativePath, directory);
    }
}
