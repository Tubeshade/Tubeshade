using System.Reflection;

namespace Tubeshade.Server.Configuration;

internal static class ApplicationInfo
{
    internal static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version is { } version
        ? $"v{version.ToString(3)}"
        : string.Empty;
}
