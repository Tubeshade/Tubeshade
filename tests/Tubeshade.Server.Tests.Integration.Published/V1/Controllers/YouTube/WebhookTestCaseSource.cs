using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Tubeshade.Data.Media;
using Tubeshade.Data.Preferences;

namespace Tubeshade.Server.Tests.Integration.Published.V1.Controllers.YouTube;

public sealed class WebhookTestCaseSource : IEnumerable<TestCaseData<string, PreferencesEntity?, VideoType?, bool>>
{
    /// <inheritdoc />
    public IEnumerator<TestCaseData<string, PreferencesEntity?, VideoType?, bool>> GetEnumerator()
    {
        yield return new(
            "https://www.youtube.com/watch?v={0}",
            null,
            null,
            false)
        {
            TestName = "New video",
        };

        yield return new(
            "https://www.youtube.com/watch?v={0}",
            new PreferencesEntity { VideosCount = 1, LiveStreamsCount = 1 },
            null,
            false)
        {
            TestName = "New video with preferences",
        };

        yield return new(
            "https://www.youtube.com/shorts/{0}",
            new PreferencesEntity { ShortsCount = 5 },
            null,
            false)
        {
            TestName = "New short",
        };

        yield return new(
            "https://www.youtube.com/shorts/{0}",
            null,
            VideoType.Short,
            false)
        {
            TestName = "Existing short",
        };

        yield return new(
            "https://www.youtube.com/watch?v={0}",
            new PreferencesEntity { VideosCount = 0, LiveStreamsCount = 0 },
            null,
            true)
        {
            TestName = "New ignored video",
        };

        yield return new(
            "https://www.youtube.com/watch?v={0}",
            new PreferencesEntity { VideosCount = 0, LiveStreamsCount = 1 },
            null,
            true)
        {
            TestName = "New ignored video, with livestream check",
        };

        yield return new(
            "https://www.youtube.com/watch?v=sWasdbDVNvc",
            new PreferencesEntity { VideosCount = 1, LiveStreamsCount = 0 },
            null,
            true)
        {
            TestName = "New ignored livestream",
        };

        yield return new(
            "https://www.youtube.com/shorts/{0}",
            new PreferencesEntity { ShortsCount = 0 },
            null,
            true)
        {
            TestName = "New ignored short",
        };
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
