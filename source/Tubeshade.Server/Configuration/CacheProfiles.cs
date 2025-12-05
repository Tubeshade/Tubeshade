using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using static Microsoft.AspNetCore.Mvc.ResponseCacheLocation;

namespace Tubeshade.Server.Configuration;

internal static class CacheProfiles
{
    internal const string Static = nameof(Static);

    internal static void AddTubeshadeProfiles(this IDictionary<string, CacheProfile> profiles)
    {
        profiles.Add(Static, new CacheProfile { Duration = (int)Duration.FromDays(30).TotalSeconds, Location = Client });
    }
}
