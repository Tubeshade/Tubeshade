using System;
using System.Collections.Generic;
using YoutubeDLSharp.Metadata;

namespace Tubeshade.Server.Services;

public static class FormatDataExtensions
{
    extension(FormatData data)
    {
        public decimal GetFrameRate()
        {
            if (data.FrameRate is not { } frameRate)
            {
                throw new InvalidOperationException("Video format is missing frame rate");
            }

            return (decimal)Math.Round(frameRate);
        }
    }

    extension(IEnumerable<FormatData> formats)
    {
        public decimal? GetTotalSize()
        {
            var total = 0m;

            foreach (var format in formats)
            {
                if ((format.FileSize ?? format.ApproximateFileSize) is not { } size)
                {
                    return null;
                }

                total += size;
            }

            return total;
        }
    }
}
