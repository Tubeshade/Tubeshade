using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace Ytdlp.Tests.Channels;

public sealed class ChannelDataTestCaseSource : IEnumerable<TestCaseData<string, PlaylistData>>
{
    /// <inheritdoc />
    public IEnumerator<TestCaseData<string, PlaylistData>> GetEnumerator()
    {
        yield return new(
            string.Empty,
            new PlaylistData());
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
