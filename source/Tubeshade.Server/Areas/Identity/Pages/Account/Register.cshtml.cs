using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Tubeshade.Data;
using Tubeshade.Data.Identity;

namespace Tubeshade.Server.Areas.Identity.Pages.Account;

[AllowAnonymous]
public sealed class RegisterModel : PageModel
{
    private readonly ILogger<RegisterModel> _logger;
    private readonly UserManager<UserEntity> _userManager;
    private readonly SignInManager _signInManager;

    public RegisterModel(
        ILogger<RegisterModel> logger,
        UserManager<UserEntity> userManager,
        SignInManager signInManager)
    {
        _logger = logger;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public string? ReturnUrl { get; set; }

    public List<AuthenticationScheme> ExternalLogins { get; set; } = [];

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Name = Input.Username,
            NormalizedName = Input.Username.NormalizeInvariant(),
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        _logger.UserCreated();
        if (_userManager.Options.SignIn.RequireConfirmedAccount)
        {
            throw new NotSupportedException();
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(returnUrl);
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public sealed class InputModel
    {
        [Required]
        public string Username { get; set; } = null!;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 16)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
