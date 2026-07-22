using System.Globalization;
using FluentAssertions;
using NUnit.Framework;
using Tubeshade.Server.Pages;

namespace Tubeshade.Server.Tests.Pages;

public sealed class NumberExtensionsTests
{
    [TestCase(1023, 0, "1023 B")]
    [TestCase(1023, 1, "1023")]
    [TestCase(10230, 0, "9.99 KiB")]
    [TestCase(10230, 1, "9.99 KiB")]
    [TestCase(10230, 2, "10230")]
    [TestCase(10230000, 0, "9.76 MiB")]
    [TestCase(10230000, 1, "9.76 MiB")]
    [TestCase(10230000, 2, "9.76 MiB")]
    [TestCase(10230000, 3, "10230000")]
    public void FormatSize(decimal value, int minimumMultiplier, string expected)
    {
        value.FormatSize(minimumMultiplier, CultureInfo.InvariantCulture).Should().Be(expected);
    }

    [TestCase(1, "1")]
    [TestCase(999, "999")]
    [TestCase(1_000, "1K")]
    [TestCase(1_005, "1.01K")]
    [TestCase(1_557, "1.56K")]
    [TestCase(999_431, "999K")]
    [TestCase(999_999, "1M")]
    [TestCase(1_000_000, "1M")]
    [TestCase(1_234_000, "1.23M")]
    [TestCase(1_789_000, "1.79M")]
    [TestCase(999_478_523, "999M")]
    [TestCase(999_538_523, "1B")]
    [TestCase(1_999_538_523, "2B")]
    public void FormatCount(decimal value, string expected)
    {
        value.FormatCount(CultureInfo.InvariantCulture).Should().Be(expected);
    }
}
