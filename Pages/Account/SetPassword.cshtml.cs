using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyInventory.Services;

namespace PropertyInventory.Pages.Account;

public class SetPasswordModel : PageModel
{
    private readonly AuthService _authService;
    private readonly ILogger<SetPasswordModel> _logger;

    public SetPasswordModel(AuthService authService, ILogger<SetPasswordModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [BindProperty]
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public void OnGet(string? email = null)
    {
        // Try to get email from query string if not provided as parameter
        if (string.IsNullOrEmpty(email))
        {
            email = Request.Query["email"].FirstOrDefault();
        }

        // Decode URL-encoded email if needed
        if (!string.IsNullOrEmpty(email))
        {
            email = Uri.UnescapeDataString(email);
        }

        if (string.IsNullOrEmpty(email))
        {
            ErrorMessage = "Email address is required. Please use the link from your approval email.";
        }
        else
        {
            Email = email.Trim();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrEmpty(Email))
        {
            // Try to get email from query string again
            Email = Request.Query["email"].FirstOrDefault() ?? string.Empty;
            if (!string.IsNullOrEmpty(Email))
            {
                Email = Uri.UnescapeDataString(Email).Trim();
            }
        }

        if (string.IsNullOrEmpty(Email))
        {
            ModelState.AddModelError(string.Empty, "Email address is required.");
            ErrorMessage = "Email address is required. Please use the link from your approval email.";
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Normalize email
        var normalizedEmail = Email.Trim().ToLowerInvariant();

        _logger.LogInformation("Setting password for email: {Email}", normalizedEmail);

        // Set password for the user
        var result = await _authService.SetPasswordAsync(normalizedEmail, Password);

        if (result.Success)
        {
            _logger.LogInformation("Password set successfully for email: {Email}", normalizedEmail);

            // Redirect to MobileLogin page - user needs to login manually
            TempData["PasswordSet"] = "Password set successfully! Please login with your new password.";
            return RedirectToPage("./MobileLogin");
        }
        else
        {
            _logger.LogWarning("Failed to set password for email: {Email}, Error: {Error}", normalizedEmail, result.ErrorMessage);
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to set password. Please try again.");
            ErrorMessage = result.ErrorMessage ?? "Failed to set password. Please try again.";
            return Page();
        }
    }
}

