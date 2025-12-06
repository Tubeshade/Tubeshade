using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace YoutubeDLSharp.Options;

/// <summary>
/// Represents a yt-dlp option that can be set multiple times.
/// </summary>
/// <typeparam name="T">The type of the option.</typeparam>
[DynamicallyAccessedMembers(All)]
public sealed class MultiOption<[DynamicallyAccessedMembers(All)] T> : IOption
{
    private MultiValue<T>? _value;

    public string DefaultOptionString => OptionStrings.Last();

    public string[] OptionStrings { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsSet { get; private set; }

    public bool IsCustom { get; }

    public MultiValue<T>? Value
    {
        get => _value;
        set
        {
            IsSet = !Equals(value, default(T));
            _value = value;
        }
    }

    public MultiOption(params string[] optionStrings)
    {
        OptionStrings = optionStrings;
        IsSet = false;
    }

    public MultiOption(bool isCustom, params string[] optionStrings)
    {
        OptionStrings = optionStrings;
        IsSet = false;
        IsCustom = isCustom;
    }

    public void SetFromString(string s)
    {
        var split = s.Split(' ');
        var stringValue = s.Substring(split[0].Length).Trim().Trim('"');
        if (!OptionStrings.Contains(split[0]))
        {
            throw new ArgumentException("Given string does not match required format.");
        }

        // Set as initial value or append to existing
        var newValue = Utils.OptionValueFromString<T>(stringValue) ?? throw new NullReferenceException();
        if (!IsSet)
        {
            Value = newValue;
        }
        else
        {
            Value.Values.Add(newValue);
        }
    }

    public override string ToString() => string.Join(" ", ToStringCollection());

    public IEnumerable<string> ToStringCollection()
    {
        if (!IsSet)
        {
            return [""];
        }

        var strings = new List<string>();
        foreach (var value in Value.Values)
        {
            strings.Add(DefaultOptionString + Utils.OptionValueToString(value));
        }

        return strings;
    }
}
