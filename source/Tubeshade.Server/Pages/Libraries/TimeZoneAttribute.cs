using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace Tubeshade.Server.Pages.Libraries;

public sealed class TimeZoneAttribute : ValidationAttribute
{
    /// <inheritdoc />
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string timeZoneId)
        {
            return null;
        }

        var provider = validationContext.GetRequiredService<IDateTimeZoneProvider>();
        return provider.Ids.Contains(timeZoneId)
            ? ValidationResult.Success
            : new ValidationResult("Not a valid time zone id");
    }
}
