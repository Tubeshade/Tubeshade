using System.Threading.Tasks;
using NUnit.Framework;
using VerifyNUnit;

namespace Ytdlp.Tests;

public sealed class VerifyChecksTests
{
    [Test]
    public async Task Run() => await VerifyChecks.Run();
}
