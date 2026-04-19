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
}
