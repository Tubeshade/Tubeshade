using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Tubeshade.Data.Identity;
using Tubeshade.Server.Configuration.Auth;

namespace Tubeshade.Server.Areas.Identity.Pages.Account;

[AllowAnonymous]
public sealed class ExternalLoginModel : PageModel
{
    private readonly ILogger<ExternalLoginModel> _logger;
    private readonly UserManager<UserEntity> _userManager;
    private readonly SignInManager _signInManager;

    public ExternalLoginModel(
        ILogger<ExternalLoginModel> logger,
        UserManager<UserEntity> userManager,
        SignInManager signInManager)
    {
        _logger = logger;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public string ProviderDisplayName { get; set; }

    public string ReturnUrl { get; set; }

    [TempData]
    public string ErrorMessage { get; set; }

    public IActionResult OnGet() => RedirectToPage("./Login");

    public IActionResult OnPost(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");

        if (remoteError is not null)
        {
            ErrorMessage = $"Error from external provider: {remoteError}";
            return RedirectToPage("./Login", new { ReturnUrl });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            ErrorMessage = "Error loading external login information.";
            return RedirectToPage("./Login", new { ReturnUrl });
        }

        ProviderDisplayName = info.ProviderDisplayName ?? info.LoginProvider;

        var result = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            false,
            true);

        if (result.Succeeded)
        {
            _logger.LogInformation("{Name} logged in with {LoginProvider} provider", info.Principal.Identity?.Name,
                info.LoginProvider);
            return LocalRedirect(ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            return RedirectToPage("./Lockout");
        }

        Input = new();

        if (info.Principal.TryGetFirstClaimValue("preferred_username", out var username))
        {
            Input.Username = username;
        }
        else if (info.Principal.TryGetFirstClaimValue(ClaimTypes.Email, out var email))
        {
            Input.Username = email;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostConfirmationAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            ErrorMessage = "Error loading external login information during confirmation.";
            return RedirectToPage("./Login", new { ReturnUrl });
        }

        ProviderDisplayName = info.ProviderDisplayName ?? info.LoginProvider;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!Guid.TryParseExact(info.ProviderKey, "D", out var id))
        {
            id = Guid.NewGuid();
        }

        var user = new UserEntity
        {
            Id = id,
            Name = Input.Username,
            NormalizedName = Input.Username,
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        result = await _userManager.AddLoginAsync(user, info);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        _logger.UserCreatedExternal(info.LoginProvider);
        if (_userManager.Options.SignIn.RequireConfirmedAccount)
        {
            throw new NotSupportedException();
        }

        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
        return LocalRedirect(ReturnUrl);
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public sealed class InputModel
    {
        [Required]
        public string Username { get; set; } = null!;
    }
}
