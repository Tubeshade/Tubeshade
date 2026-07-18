using System;
using Microsoft.AspNetCore.Mvc;
using Tubeshade.Data.Media.Channels;

namespace Tubeshade.Server.Pages.Channels;

public sealed record ChannelModel(DetailedChannel Channel, Guid? LibraryId)
{
    public string? GetThumbnailSource(IUrlHelper urlHelper) => Channel.Thumbnails.GetSource(urlHelper);

    public string GetThumbnailSourceSet(IUrlHelper urlHelper) => Channel.Thumbnails.GetSourceSet(urlHelper);
}
