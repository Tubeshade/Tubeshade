using System.ComponentModel.DataAnnotations;

namespace Tubeshade.Server.Pages.Libraries.Downloads;

public sealed class DownloadVideoModel
{
    [Required]
    public string Url { get; set; } = null!;
}
