using System;
using System.Collections;
using NodaTime;
using NUnit.Framework;
using SponsorBlock;
using Tubeshade.Data.Media;

namespace Tubeshade.Server.Tests.Pages.Shared;

public sealed class GetTotalDurationTestCaseSource : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        yield return new TestCaseData(
                Array.Empty<SponsorBlockSegmentEntity>(),
                Period.Zero)
            .SetName("Zero segments");

        yield return new TestCaseData(
                new SponsorBlockSegmentEntity[]
                {
                    new()
                    {
                        VideoId = Guid.Empty,
                        ExternalId = string.Empty,
                        StartTime = 5.0m,
                        EndTime = 10.0m,
                        Category = SegmentCategory.Interaction,
                        Action = SegmentAction.Skip,
                        Locked = true,
                    },
                },
                Period.FromSeconds(5))
            .SetName("One segment");

        yield return new TestCaseData(
                new SponsorBlockSegmentEntity[]
                {
                    new()
                    {
                        VideoId = Guid.Empty,
                        ExternalId = string.Empty,
                        StartTime = 0.0m,
                        EndTime = 0.0m,
                        Category = SegmentCategory.Sponsor,
                        Action = SegmentAction.Full,
                        Locked = true,
                    },
                },
                Period.Zero)
            .SetName("Full video segments will always have start and end time as 0");

        yield return new TestCaseData(
                new SponsorBlockSegmentEntity[]
                {
                    new()
                    {
                        VideoId = Guid.Empty,
                        ExternalId = string.Empty,
                        StartTime = 5.0m,
                        EndTime = 10.0m,
                        Category = SegmentCategory.Interaction,
                        Action = SegmentAction.Skip,
                        Locked = true,
                    },
                    new()
                    {
                        VideoId = Guid.Empty,
                        ExternalId = string.Empty,
                        StartTime = 15.0m,
                        EndTime = 20.0m,
                        Category = SegmentCategory.Interaction,
                        Action = SegmentAction.Skip,
                        Locked = true,
                    },
                },
                Period.FromSeconds(10))
            .SetName("Non-overlapping segments are added together");

        yield return new TestCaseData(
                new SponsorBlockSegmentEntity[]
                {
                    new()
                    {
                        VideoId = Guid.Empty,
                        ExternalId = string.Empty,
                        StartTime = 5.0m,
                        EndTime = 20.0m,
                        Category = SegmentCategory.Interaction,
                        Action = SegmentAction.Skip,
                        Locked = true,
                    },
                    new()
                    {
                        VideoId = Guid.Empty,
                        ExternalId = string.Empty,
                        StartTime = 15.0m,
                        EndTime = 19.0m,
                        Category = SegmentCategory.Interaction,
                        Action = SegmentAction.Skip,
                        Locked = true,
                    },
                },
                Period.FromSeconds(15))
            .SetName("Overlapping segments are not counted twice");

        yield return new TestCaseData(
                new SponsorBlockSegmentEntity[]
                {
                    new()
                    {
                        VideoId = Guid.Empty,
                        ExternalId = string.Empty,
                        StartTime = 5.0m,
                        EndTime = 20.0m,
                        Category = SegmentCategory.Interaction,
                        Action = SegmentAction.Skip,
                        Locked = true,
                    },
                    new()
                    {
                        VideoId = Guid.Empty,
                        ExternalId = string.Empty,
                        StartTime = 15.0m,
                        EndTime = 25.0m,
                        Category = SegmentCategory.Interaction,
                        Action = SegmentAction.Skip,
                        Locked = true,
                    },
                },
                Period.FromSeconds(20))
            .SetName("Overlapping segments are counted correctly");
    }
}
