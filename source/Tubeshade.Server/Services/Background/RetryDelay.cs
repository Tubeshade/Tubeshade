using System;

namespace Tubeshade.Server.Services.Background;

internal static class RetryDelay
{
    internal static TimeSpan GetExponential(int attempt)
    {
        return TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, attempt)));
    }
}
