using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Tubeshade.Data.Media.Channels;

namespace Tubeshade.Server.Pages.Creators;

public sealed class CreateCreatorModel
{
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public Guid? PrimaryChannelId { get; set; }

    [Browsable(false)]
    internal IEnumerable<ChannelEntity> Channels { get; set; } = [];
}

public sealed class EditCreatorModel
{
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public Guid? PrimaryChannelId { get; set; }

    public Guid[]? ChannelIds { get; set; }

    [Browsable(false)]
    internal IEnumerable<ChannelEntity> Channels { get; set; } = [];
}
