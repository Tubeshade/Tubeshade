using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Tubeshade.Server.Pages.Libraries;

public sealed class AddLibraryModel : IValidatableObject
{
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string StoragePath { get; set; } = null!;

    [Required]
    [CronExpression]
    public string CronExpression { get; set; } = "0 5 * * *";

    [Required]
    [TimeZone]
    public string TimeZoneId { get; set; } = "Etc/UTC";

    [Browsable(false)]
    internal IEnumerable<string> TimeZoneIds { get; init; } = [];

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Directory.Exists(StoragePath))
        {
            return [new ValidationResult("Directory does not exist", [nameof(StoragePath)])];
        }

        try
        {
            var testFilePath = Path.Combine(StoragePath, Guid.NewGuid().ToString("D"));
            using (File.Create(testFilePath))
            {
            }

            File.Delete(testFilePath);
        }
        catch (Exception)
        {
            return [new ValidationResult("Could not create a file in directory", [nameof(StoragePath)])];
        }

        return [];
    }
}
