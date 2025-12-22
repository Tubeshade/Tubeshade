using System.Linq;
using YoutubeDLSharp.Options;

namespace Ytdlp;

public static class OptionSetExtensions
{
    public static string ToArguments(this OptionSet options, string url)
    {
        return string.Join(' ', options.GetOptionFlags().Append(url));
    }
}
