using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Tubeshade.Server.Tests.Integration.Published.Fixtures;

namespace Tubeshade.Server.Tests.Integration.Published.Pages;

public sealed class SmokeTests(IServerFixture serverFixture) : PlaywrightTests(serverFixture)
{
    [Test]
    [TestCase("/", "Home page - Tubeshade")]
    [TestCase("/Libraries", "Libraries - Tubeshade")]
    [TestCase("/Channels", "Channels - Tubeshade")]
    [TestCase("/Downloads", "Downloads - Tubeshade")]
    [TestCase("/Tasks", "Tasks - Tubeshade")]
    [TestCase("/Identity/Account/Manage", "Profile - Tubeshade")]
    [TestCase("/Identity/Account/Manage/SetPassword", "Change password - Tubeshade")]
    [TestCase("/Identity/Account/Manage/ExternalLogins", "Manage your external logins - Tubeshade")]
    [TestCase("/Identity/Account/Manage/Preferences", "Preferences - Tubeshade")]
    public async Task Navigate(string url, string title)
    {
        await Page.GotoAsync(url);
        (await Page.TitleAsync()).Should().Be(title);
    }
}
