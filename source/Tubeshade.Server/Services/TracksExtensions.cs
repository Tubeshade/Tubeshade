using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Tubeshade.Data.Media;

namespace Tubeshade.Server.Services;

public static class TracksExtensions
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    extension(IEnumerable<TrackFileEntity> tracks)
    {
        public bool TryGetChapters([MaybeNullWhen(false)] out TrackFileEntity chapters)
        {
            chapters = tracks.SingleOrDefault(file => file.Type == TrackType.Chapters);
            return chapters is not null;
        }

        public bool TryGetSubtitles(
            string language,
            [MaybeNullWhen(false)] out TrackFileEntity subtitles)
        {
            subtitles = tracks.SingleOrDefault(file =>
                file.Type == TrackType.Subtitles &&
                Comparer.Equals(file.Language, language));

            return subtitles is not null;
        }
    }
}
