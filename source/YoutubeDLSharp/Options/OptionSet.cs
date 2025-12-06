using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace YoutubeDLSharp.Options;

/// <summary>
/// Represents a set of options for yt-dlp.
/// </summary>
[DynamicallyAccessedMembers(All)]
public sealed partial class OptionSet : ICloneable
{
    private static readonly OptionComparer Comparer = new();

    /// <summary>The default option set (if no options are explicitly set).</summary>
    public static readonly OptionSet Default = new();

    /// <summary>Creates an option set from an array of command-line option strings.</summary>
    /// <param name="lines">An array containing one command-line option string per item.</param>
    public static OptionSet FromString(IEnumerable<string> lines)
    {
        var optSet = new OptionSet();

        var customOptions = GetOptions(lines, optSet.GetKnownOptions())
            .Where(option => option.IsCustom)
            .ToArray();

        optSet.CustomOptions = customOptions;

        return optSet;
    }

    /// <summary>Loads an option set from a yt-dlp config file.</summary>
    /// <param name="path">The path to the config file.</param>
    public static OptionSet LoadConfigFile(string path) => FromString(File.ReadAllLines(path));

    /// <summary>
    /// Writes all options to a config file with the specified path.
    /// </summary>
    public void WriteConfigFile(string path) => File.WriteAllLines(path, GetOptionFlags());

    /// <summary>Returns a collection of all option flags.</summary>
    public IEnumerable<string> GetOptionFlags() => GetKnownOptions()
        .Concat(CustomOptions)
        .SelectMany(option => option.ToStringCollection())
        .Where(value => !string.IsNullOrWhiteSpace(value));

    /// <summary>
    /// Creates a clone of this option set and overrides all options with non-default values set in the given option set.
    ///
    /// Note: Only overriding non-default values might cause some unintuitive behavior, e.g. for bool options, where "false" is the default.
    /// Use forceOverride to force overriding also with default values.
    /// </summary>
    /// <param name="overrideOptions">All non-default option values of this option set will be copied to the cloned option set.</param>
    /// <param name="forceOverride">Force overriding also default values.</param>
    /// <returns>A cloned option set with all specified options overriden.</returns>
    public OptionSet OverrideOptions(OptionSet overrideOptions, bool forceOverride = false)
    {
        var cloned = (OptionSet)Clone();
        cloned.CustomOptions = cloned.CustomOptions
            .Concat(overrideOptions.CustomOptions)
            .Distinct(Comparer)
            .ToArray();

        foreach (var field in GetKnownOptionFields(overrideOptions))
        {
            var fieldValue = (IOption?)field.GetValue(overrideOptions);
            if (forceOverride || (fieldValue?.IsSet ?? false))
            {
                typeof(OptionSet)
                    .GetField(field.Name, BindingFlags.NonPublic | BindingFlags.Instance)?
                    .SetValue(cloned, fieldValue);
            }
        }

        return cloned;
    }

    /// <inheritdoc />
    public override string ToString() => $" {string.Join(" ", GetOptionFlags())}";

    /// <inheritdoc />
    public object Clone() => FromString(GetOptionFlags());

    internal IEnumerable<IOption> GetKnownOptions() => GetKnownOptionFields(this)
        .Select(field => field.GetValue(this))
        .Cast<IOption>();

    private static IEnumerable<IOption> GetOptions(IEnumerable<string> lines, IEnumerable<IOption> options)
    {
        IEnumerable<IOption> knownOptions = options.ToList();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // skip comments
            if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var segments = line.Split(' ');
            var flag = segments[0];

            var knownOption = knownOptions.FirstOrDefault(o => o.OptionStrings.Contains(flag));
            IOption customOption = segments.Length > 1
                ? new Option<string>(isCustom: true, flag)
                : new Option<bool>(isCustom: true, flag);

            var option = knownOption ?? customOption;

            option.SetFromString(line);
            yield return option;
        }
    }

    private static IEnumerable<FieldInfo> GetKnownOptionFields<[DynamicallyAccessedMembers(All)] TValue>(TValue value)
        where TValue : class
    {
        return typeof(TValue)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(field =>
                field.FieldType.IsGenericType &&
#pragma warning disable IL2075
                field.FieldType.GetInterfaces().Contains(typeof(IOption)));
#pragma warning restore IL2075
    }
}
