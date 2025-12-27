using System.ComponentModel.DataAnnotations;
using Tubeshade.Data.Media;

namespace Tubeshade.Server.Pages.Libraries.Channels;

public class CreateChannelModel
{
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string ExternalId { get; set; } = null!;

    [Required]
    public string ExternalUrl { get; set; } = null!;

    [Required]
    public ExternalAvailability Availability { get; set; } = null!;
}
