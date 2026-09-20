using System.Threading.Tasks;
using NUnit.Framework;
using VerifyNUnit;

namespace Tubeshade.Server.Tests.Integration.Published;

public sealed class VerifyChecksTests
{
    [Test]
    public async Task Run() => await VerifyChecks.Run();
}
