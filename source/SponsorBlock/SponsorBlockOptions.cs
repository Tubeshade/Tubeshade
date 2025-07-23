using System.ComponentModel.DataAnnotations;

namespace SponsorBlock;

public sealed class SponsorBlockOptions
{
    [Required]
    public string BaseUrl { get; set; } = "https://sponsor.ajay.app/";
}
