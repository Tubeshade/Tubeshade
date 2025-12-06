using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace YoutubeDLSharp.Options;

internal static class Utils
{
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "All used types will be annotated correctly using Option<T>")]
    internal static T? OptionValueFromString<[DynamicallyAccessedMembers(All)] T>(string stringValue)
    {
        if (typeof(T) == typeof(bool))
        {
            return (T)(object)true;
        }

        if (typeof(T) == typeof(Enum))
        {
            var titleCase = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(stringValue);
            return (T)Enum.Parse(typeof(T), titleCase);
        }

        if (typeof(T) == typeof(DateTime))
        {
            return (T)(object)DateTime.ParseExact(stringValue, "yyyyMMdd", null);
        }

        var conv = TypeDescriptor.GetConverter(typeof(T));
        return (T?)conv.ConvertFrom(stringValue);
    }

    internal static string OptionValueToString<T>(T value)
    {
        string val;
        if (value is bool)
        {
            val = string.Empty;
        }
        else if (value is Enum)
        {
            val = $" \"{value.ToString()?.ToLower()}\"";
        }
        else if (value is DateTime dateTime)
        {
            val = $" {dateTime:yyyyMMdd}";
        }
        else if (value is string)
        {
            val = $" \"{value}\"";
        }
        else
        {
            val = $" {value}";
        }

        return val;
    }
}
