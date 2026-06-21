using System;
using System.Globalization;
using System.Web;

namespace Tubeshade.Server.Pages.Libraries.Videos;

public static class StringExtensions
{
    public static double? GetTimeParameter(this string uri)
    {
        var queryParameters = HttpUtility.ParseQueryString(new UriBuilder(uri).Query);
        if (queryParameters["t"] is not { } time)
        {
            return null;
        }

        if (int.TryParse(time, CultureInfo.InvariantCulture, out var seconds))
        {
            return seconds;
        }

        if (HumanReadablePeriodPattern.Instance.Parse(time) is { Success: true } result)
        {
            return result.Value.ToDuration().TotalSeconds;
        }

        return null;
    }
}
