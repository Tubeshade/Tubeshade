using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Tubeshade.Data.Media;

namespace Tubeshade.Server.Pages.Playlists;

public sealed class CreatePlaylistModel
{
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public Guid LibraryId { get; set; }

    [Required]
    public string ExternalId { get; set; } = null!;

    [Required]
    public string ExternalUrl { get; set; } = null!;

    [Browsable(false)]
    internal IEnumerable<LibraryEntity> Libraries { get; set; } = [];
}
