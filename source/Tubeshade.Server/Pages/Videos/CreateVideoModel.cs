using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NodaTime;
using Tubeshade.Data.Media;
using Tubeshade.Server.Pages.Libraries;

namespace Tubeshade.Server.Pages.Videos;

public sealed class CreateVideoModel
{
    // lang=regex
    public const string CommaSeparatedRegexPattern = "^[a-zA-Z]+(,[a-zA-Z]+)*$";

    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public Guid? ChannelId { get; set; }

    [Required]
    public VideoType Type { get; set; } = null!;


    [Required]
    public string ExternalId { get; set; } = null!;

    [Required]
    public string ExternalUrl { get; set; } = null!;

    [Required]
    public ExternalAvailability Availability { get; set; } = null!;


    [Required]
    public LocalDateTime? PublishedAt { get; set; }

    [Required]
    [TimeZone]
    public string PublishedAtTimeZone { get; set; } = "Etc/UTC";

    [Required]
    public decimal? DurationInSeconds { get; set; }


    public string? Description { get; set; }

    [RegularExpression(CommaSeparatedRegexPattern)]
    public string? Categories { get; set; }

    [RegularExpression(CommaSeparatedRegexPattern)]
    public string? Tags { get; set; }


    [Browsable(false)]
    internal IEnumerable<ChannelEntity> Channels { get; set; } = [];

    [Browsable(false)]
    internal IEnumerable<string> TimeZoneIds { get; set; } = [];
}
