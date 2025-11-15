using System.Threading.Tasks;

namespace Tubeshade.Server.Tests.Integration.Published.Pages;

public sealed class Pages : TubeshadePageTest
{
    [Test]
    [Arguments("/", "Home page - Tubeshade")]
    [Arguments("/Libraries", "Libraries - Tubeshade")]
    [Arguments("/Channels", "Channels - Tubeshade")]
    [Arguments("/Downloads", "Downloads - Tubeshade")]
    [Arguments("/Tasks", "Tasks - Tubeshade")]
    [Arguments("/Identity/Account/Manage", "Profile - Tubeshade")]
    [Arguments("/Identity/Account/Manage/SetPassword", "Change password - Tubeshade")]
    [Arguments("/Identity/Account/Manage/ExternalLogins", "Manage your external logins - Tubeshade")]
    [Arguments("/Identity/Account/Manage/Preferences", "Preferences - Tubeshade")]
    public async Task Navigate(string url, string title)
    {
        await Page.GotoAsync(url);
        await Assert.That(Page.TitleAsync()).IsEqualTo(title);
    }
}
