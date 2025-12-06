using System.Collections.Generic;

namespace YoutubeDLSharp.Options;

internal class OptionComparer : IEqualityComparer<IOption>
{
    public bool Equals(IOption? x, IOption? y)
    {
        if (x is not null)
        {
            return y != null && string.Equals(x.ToString(), y.ToString());
        }

        return y == null;
    }

    public int GetHashCode(IOption obj) => obj.ToString()?.GetHashCode() ?? 0;
}
