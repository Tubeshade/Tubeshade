using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Tubeshade.Server.Configuration;

internal static class StringExtensions
{
    extension(string? path)
    {
        [return: NotNullIfNotNull(nameof(path))]
        internal string? GetFullPath() => string.IsNullOrWhiteSpace(path)
            ? path
            : Path.GetFullPath(path);
    }
}
