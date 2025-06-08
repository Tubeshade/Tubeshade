using System.ComponentModel.DataAnnotations;

namespace Tubeshade.Server.Pages.Libraries;

public sealed class UpdatePreferencesModel
{
    [Required]
    public decimal? PlaybackSpeed { get; set; }
}
