using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Tubeshade.Server.V1.Models;

public sealed class IntentVerificationRequest
{
    [Required]
    [FromQuery(Name = "hub.mode")]
    public required string Mode { get; set; }

    [Required]
    [FromQuery(Name = "hub.topic")]
    public required Uri Topic { get; set; }

    [Required]
    [FromQuery(Name = "hub.challenge")]
    public required string Challenge { get; set; }

    [FromQuery(Name = "hub.verify_token")]
    public string? VerifyToken { get; set; }

    [FromQuery(Name = "hub.lease_seconds")]
    public int? LeaseSeconds { get; set; }
}
