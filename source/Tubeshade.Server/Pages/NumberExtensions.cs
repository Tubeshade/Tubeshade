using System;
using System.Globalization;

namespace Tubeshade.Server.Pages;

public static class NumberExtensions
{
    private const decimal UnitMultiplier = 1024;

    private static readonly string[] Units =
    [
        "B",
        "KiB",
        "MiB",
        "GiB",
        "TiB",
        "PiB",
    ];

    extension(decimal value)
    {
        public string FormatSize(int minimumMultiplier, CultureInfo cultureInfo)
        {
            var normalized = value;
            var unitIndex = 0;
            while (normalized >= UnitMultiplier && unitIndex + 1 < Units.Length)
            {
                normalized /= UnitMultiplier;
                unitIndex++;
            }

            if (unitIndex < minimumMultiplier)
            {
                return Math.Round(value, 2).ToString(cultureInfo);
            }

            var unit = Units[unitIndex];

            return $"{Math.Round(normalized, 2).ToString(cultureInfo)} {unit}";
        }
    }
}
