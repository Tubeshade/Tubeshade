using System.Globalization;
using AwesomeAssertions;
using NUnit.Framework;
using Tubeshade.Server.Pages;

namespace Tubeshade.Server.Tests.Pages;

public sealed class NumberExtensionsTests
{
    [TestCase(0, "0 B")]
    [TestCase(1023, "1023 B")]
    [TestCase(1024, "1 KiB")]
    [TestCase(10230, "9.99 KiB")]
    [TestCase(10230000, "9.76 MiB")]
    public void FormatSize(decimal value, string expected)
    {
        value.FormatSize(CultureInfo.InvariantCulture).Should().Be(expected);
    }

    [TestCase(0, 10230000, "0 MiB")]
    [TestCase(1023, 10230000, "0 MiB")]
    [TestCase(10230, 10230000, "0.01 MiB")]
    [TestCase(10230000, 10230000, "9.76 MiB")]
    [TestCase(10230000, 1023, "10230000 B")]
    public void FormatSize_WithReference(decimal value, decimal reference, string expected)
    {
        value.FormatSize(reference, CultureInfo.InvariantCulture).Should().Be(expected);
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
