using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NodaTime.Text;

namespace Tubeshade.Server.Services;

public sealed class WebVideoTextTracksService
{
    private static readonly DurationPattern Pattern = DurationPattern.CreateWithInvariantCulture("HH:mm:ss.fff");
    private static readonly Encoding Encoding = Encoding.UTF8;

    public async ValueTask Write(
        Stream stream,
        IEnumerable<TextTrackCue> cues,
        CancellationToken cancellationToken = default)
    {
        await using var writer = new StreamWriter(stream, Encoding, leaveOpen: true);
        await writer.WriteLineAsync("WEBVTT".AsMemory(), cancellationToken);

        foreach (var (index, cue) in cues.Index())
        {
            await writer.WriteLineAsync(
                $"""

                     {index + 1}
                     {Pattern.Format(cue.StartTime)} --> {Pattern.Format(cue.EndTime)}
                     {cue.Text}
                     """
                    .AsMemory(),
                cancellationToken);
        }
    }
}
