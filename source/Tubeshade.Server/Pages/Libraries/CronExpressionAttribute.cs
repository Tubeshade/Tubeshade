using System.ComponentModel.DataAnnotations;
using Cronos;

namespace Tubeshade.Server.Pages.Libraries;

public sealed class CronExpressionAttribute : ValidationAttribute
{
    /// <inheritdoc />
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string expression)
        {
            return null;
        }

        return CronExpression.TryParse(expression, out _)
            ? ValidationResult.Success
            : new ValidationResult("Not a valid cron expression");
    }
}
