using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Tubeshade.Server.Tests.Integration.Published.Fixtures;

public sealed class ServerFixtureSource : IEnumerable<TestFixtureData>
{
    /// <inheritdoc />
    public IEnumerator<TestFixtureData> GetEnumerator()
    {
        return ServerSetup
            .Fixtures
            .Select(fixture => new TestFixtureData(fixture).SetArgDisplayNames(fixture.Name))
            .GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
