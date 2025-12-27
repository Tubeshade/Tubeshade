using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Tubeshade.Data.Media;

namespace Tubeshade.Server.Pages.Channels;

public sealed class CreateChannelModel : Libraries.Channels.CreateChannelModel
{
    [Required]
    public Guid? LibraryId { get; set; }

    [Browsable(false)]
    internal IEnumerable<LibraryEntity> Libraries { get; set; } = [];
}
