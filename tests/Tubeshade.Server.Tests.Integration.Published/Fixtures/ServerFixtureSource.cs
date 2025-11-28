using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace Tubeshade.Server.Tests.Integration.Published.Fixtures;

public sealed class ServerFixtureSource : IEnumerable<TestFixtureData>
{
    /// <inheritdoc />
    public IEnumerator<TestFixtureData> GetEnumerator()
    {
        foreach (var serverFixture in ServerSetup.Fixtures)
        {
            yield return new TestFixtureData(serverFixture).SetArgDisplayNames(serverFixture.Name);
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
