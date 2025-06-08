using System.ComponentModel.DataAnnotations;

namespace Tubeshade.Server.Pages.Libraries;

public sealed class AddLibraryModel
{
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string StoragePath { get; set; } = null!;
}
