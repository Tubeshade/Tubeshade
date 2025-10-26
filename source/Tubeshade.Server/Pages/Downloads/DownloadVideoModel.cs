using System.ComponentModel.DataAnnotations;

namespace Tubeshade.Server.Pages.Downloads;

public sealed class DownloadVideoModel
{
    [Required]
    public string Url { get; set; } = null!;
}
