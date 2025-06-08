using System;
using System.ComponentModel.DataAnnotations;

namespace Tubeshade.Server.V1.Models;

public sealed class CookieUpdateRequest
{
    [Required]
    public Guid? LibraryId { get; set; }

    [Required]
    public string Domain { get; set; } = null!;

    [Required]
    public string Cookie { get; set; } = null!;
}
