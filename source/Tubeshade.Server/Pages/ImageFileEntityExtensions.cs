using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Tubeshade.Data.Media;

namespace Tubeshade.Server.Pages;

public static class ImageFileEntityExtensions
{
    extension(ImageFileEntity[] images)
    {
        public string? GetSource(IUrlHelper urlHelper)
        {
            var thumbnail = images
                .OrderByDescending(image => image.Width)
                .ThenByDescending(image => image.Height)
                .FirstOrDefault();

            if (thumbnail is null)
            {
                return null;
            }

            return urlHelper.Action("Get", "Images", new { version = "1.0", id = thumbnail.Id });
        }

        public string GetSourceSet(IUrlHelper urlHelper)
        {
            if (images.OrderByDescending(image => image.StorageSize).ToArray() is not { Length: > 0 } thumbnails)
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
}
