using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tubeshade.Server.Pages.Libraries;

namespace Tubeshade.Server.Pages.Shared;

public interface ISettingsPage : IFormLayout
{
    UpdatePreferencesModel? UpdatePreferencesModel { get; }

    Task<IActionResult> OnPostUpdatePreferences();
}
