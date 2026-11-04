using System.ComponentModel.DataAnnotations;

namespace Tubeshade.Server.V1.Models;

public sealed class PlaybackActivityRequest
{
    private const double MaximumTimeDelta = 24 * 60 * 60;

    [Required]
    [Range(0, double.MaxValue)]
    public double? Position { get; set; }

    [Required]
    [Range(0, MaximumTimeDelta)]
    public double? WatchTimeDelta { get; set; }

    [Required]
    [Range(0, MaximumTimeDelta)]
    public double? PlaybackTimeDelta { get; set; }
}
