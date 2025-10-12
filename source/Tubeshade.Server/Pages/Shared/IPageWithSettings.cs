using Microsoft.AspNetCore.Mvc;

namespace Tubeshade.Server.Pages.Shared;

public interface IPageWithSettings
{
    IActionResult OnGetSettings();
}
