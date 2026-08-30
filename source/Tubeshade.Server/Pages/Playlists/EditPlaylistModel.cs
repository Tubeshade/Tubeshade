using System.ComponentModel.DataAnnotations;

namespace Tubeshade.Server.Pages.Playlists;

public sealed class EditPlaylistModel
{
    [Required]
    public string Name { get; set; } = null!;
}
