using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyInventory.Models;
using PropertyInventory.Services;

namespace PropertyInventory.Pages.Account;

public class TemporaryLoginModel : PageModel
{
    private readonly AuthService _authService;
    private readonly ILogger<TemporaryLoginModel> _logger;

    public TemporaryLoginModel(AuthService authService, ILogger<TemporaryLoginModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [BindProperty]
    [Required(ErrorMessage = "Temporary password is required.")]
    [Display(Name = "Temporary Password")]
    public string TemporaryPassword { get; set; } = string.Empty;

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

        _logger.LogInformation("Attempting login with temporary password for email: {Email}", normalizedEmail);

        // Attempt login with temporary password
        var (success, user) = await _authService.LoginAsync(normalizedEmail, TemporaryPassword, false);

        if (success && user != null)
        {
            _logger.LogInformation("User logged in with temporary password: {Email}", normalizedEmail);
            
            // Get user type
            // For Mobile Users, go directly to Mobile Dashboard
            // Password change can be done later from Profile page
            if (user.UserType == UserType.MobileUser)
            {
                return RedirectToPage("/Mobile/Dashboard");
            }
            
            // For Admin users, check if password change is required
            if (user.RequiresPasswordChange)
            {
                return RedirectToPage("./ChangePassword");
            }
            
            // Otherwise, go to Index (Admin Dashboard)
            return RedirectToPage("/Index");
        }
        else
        {
            _logger.LogWarning("Login failed for email: {Email}", normalizedEmail);
            ModelState.AddModelError(string.Empty, "Invalid temporary password. Please check your email and try again.");
            ErrorMessage = "Invalid temporary password. Please check your email and try again.";
            return Page();
        }
    }
}

