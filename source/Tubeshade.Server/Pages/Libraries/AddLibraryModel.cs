using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Tubeshade.Server.Pages.Libraries;

public sealed class AddLibraryModel
{
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string StoragePath { get; set; } = null!;

    [Required]
    public string CronExpression { get; set; } = "0 5 * * *";

    [Required]
    public string TimeZoneId { get; set; } = "Etc/UTC";

    [Browsable(false)]
    internal IEnumerable<string> TimeZoneIds { get; init; } = [];
}
