using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace Tubeshade.Server.Tests.Integration.Published.Fixtures;

public sealed class LocalizedServerFixtureSource : IEnumerable<TestFixtureData>
{
    private static readonly string[] Cultures = [PlaywrightTests.DefaultCulture, "lv"];

    /// <inheritdoc />
    public IEnumerator<TestFixtureData> GetEnumerator()
    {
        foreach (var serverFixture in ServerSetup.Fixtures)
        {
            foreach (var culture in Cultures)
            {
                yield return new TestFixtureData(serverFixture, culture)
                    .SetArgDisplayNames(serverFixture.Name, culture);
            }
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
