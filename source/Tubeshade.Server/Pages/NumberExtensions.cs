using System;
using System.Globalization;

namespace Tubeshade.Server.Pages;

public static class NumberExtensions
{
    private const decimal BinaryMultiplier = 1024;
    private const long DecimalMultiplier = 1000;

    private static readonly string[] BinaryUnits = ["B", "KiB", "MiB", "GiB", "TiB", "PiB"];
    private static readonly char[] DecimalUnits = ['K', 'M', 'B'];

    extension(decimal value)
    {
        public string FormatSize(int minimumMultiplier, CultureInfo cultureInfo)
        {
            var normalized = value;
            var unitIndex = 0;
            while (normalized >= BinaryMultiplier && unitIndex + 1 < BinaryUnits.Length)
            {
                normalized /= BinaryMultiplier;
                unitIndex++;
            }

            if (unitIndex < minimumMultiplier)
            {
                return Math.Round(value, 2).ToString(cultureInfo);
            }

            var unit = BinaryUnits[unitIndex];

            return $"{Math.Round(normalized, 2).ToString(cultureInfo)} {unit}";
        }

        public string FormatCount(CultureInfo cultureInfo)
        {
            const int significantFigures = 3;

            var mantissa = value;
            var multiplier = 1L;
            while (mantissa >= 10)
            {
                mantissa /= 10;
                multiplier *= 10;
            }

            mantissa = Math.Round(mantissa, significantFigures - 1, MidpointRounding.AwayFromZero);
            if (mantissa >= 10)
            {
                mantissa /= 10;
                multiplier *= 10;
            }

            var unitIndex = -1;
            while (multiplier >= DecimalMultiplier && unitIndex + 1 < DecimalUnits.Length)
            {
                multiplier /= DecimalMultiplier;
                unitIndex++;
            }

            var formatted = (mantissa * multiplier).ToString($"G{significantFigures}", cultureInfo);

            return unitIndex < 0
                ? formatted
                : $"{formatted}{DecimalUnits[unitIndex]}";
        }
    }

    extension(int value)
    {
        public string FormatCount(CultureInfo cultureInfo) => ((decimal)value).FormatCount(cultureInfo);
    }

    extension(long value)
    {
        public string FormatCount(CultureInfo cultureInfo) => ((decimal)value).FormatCount(cultureInfo);
    }
}
