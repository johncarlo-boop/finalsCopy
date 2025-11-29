using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyInventory.Models;
using PropertyInventory.Services;

namespace PropertyInventory.Pages.Account;

public class LoginModel : PageModel
{
    private readonly AuthService _authService;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(AuthService authService, ILogger<LoginModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    [TempData]
    public string? RegistrationSuccess { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public void OnGet(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        ReturnUrl = returnUrl ?? Url.Content("~/");
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (ModelState.IsValid)
        {
            var (success, user) = await _authService.LoginAsync(Input.Email, Input.Password, Input.RememberMe);

            if (success && user != null)
            {
                _logger.LogInformation("User logged in: {Email}", Input.Email);
                
                // Check if password change is required
                if (user.RequiresPasswordChange)
                {
                    return RedirectToPage("./ChangePassword");
                }
                
                // Redirect based on user type
                if (user.UserType == UserType.MobileUser)
                {
                    // MobileUser should use MobileLogin page, redirect them there
                    await _authService.LogoutAsync();
                    TempData["ErrorMessage"] = "This account is for mobile users. Please use the Mobile Login page.";
                    return RedirectToPage("./MobileLogin");
                }
                
                // For Admin users, redirect to Index page (dashboard)
                if (string.IsNullOrEmpty(returnUrl) || returnUrl == "/" || returnUrl == "~/")
                {
                    return RedirectToPage("/Index");
                }
                
                return LocalRedirect(returnUrl);
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt. Admin access only.");
                return Page();
            }
        }

        return Page();
    }
}

