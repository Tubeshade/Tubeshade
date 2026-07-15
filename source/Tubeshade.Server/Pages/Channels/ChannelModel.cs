using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Tubeshade.Data.Media.Channels;

namespace Tubeshade.Server.Pages.Channels;

public sealed record ChannelModel(DetailedChannel Channel, Guid? LibraryId)
{
    public string? GetThumbnailSource(IUrlHelper urlHelper)
    {
        var thumbnail = Channel
            .Thumbnails
            .OrderByDescending(image => image.Width)
            .ThenByDescending(image => image.Height)
            .First();

        return urlHelper.Action("Get", "Images", new { version = "1.0", id = thumbnail.Id });
    }

    public string GetThumbnailSourceSet(IUrlHelper urlHelper)
    {
        if (Channel.Thumbnails is not { Length: > 0 } thumbnails)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        for (var index = 0; index < thumbnails.Length; index++)
        {
            if (index is not 0)
            {
                builder.Append(", ");
            }

            var image = thumbnails[index];
            builder.Append(urlHelper.Action("Get", "Images", new { version = "1.0", id = image.Id }));
            builder.Append(' ');
            builder.Append(image.Width);
            builder.Append('w');
        }

        return builder.ToString();
    }
}
