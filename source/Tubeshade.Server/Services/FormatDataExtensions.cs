using System;
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
}
