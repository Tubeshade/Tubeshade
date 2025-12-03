using System;
using Microsoft.Extensions.Options;

namespace Tubeshade.Server.Tests;

internal sealed class MockOptionsMonitor<TOptions> : OptionsMonitor<TOptions>
    where TOptions : class
{
    public MockOptionsMonitor(Action<TOptions> configure)
        : base(new MockOptionsFactory(configure), [], new OptionsCache<TOptions>())
    {
    }

    private sealed class MockOptionsFactory : OptionsFactory<TOptions>
    {
        public MockOptionsFactory(Action<TOptions> configure)
            : base([new ConfigureOptions<TOptions>(configure)], [], [])
        {
        }
    }
}
