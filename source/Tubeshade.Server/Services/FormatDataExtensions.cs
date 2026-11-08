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

        public bool IsAudioOnly() => data.Resolution is "audio only";

        public bool IsWebMAudio() => data.IsAudioOnly() && data.AudioCodec is "opus" or "vorbis";

        public bool HasVideoAndAudio() => !data.IsAudioOnly() && data.AudioCodec is not (null or "none");
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
