using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace Tubeshade.Data.Tasks;

public sealed class TaskType : SmartEnum<TaskType>
{
    public static readonly TaskType Index = new(Names.Index, 1);
    public static readonly TaskType DownloadVideo = new(Names.DownloadVideo, 2);

    private TaskType(string name, int value)
        : base(name, value)
    {
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Names
    {
        public const string Index = "index";
        public const string DownloadVideo = "download_video";
    }
}
