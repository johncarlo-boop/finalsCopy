using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyInventory.Models;
using PropertyInventory.Services;

namespace PropertyInventory.Pages.Account;

public class MobileLoginModel : PageModel
{
    private readonly AuthService _authService;
    private readonly ILogger<MobileLoginModel> _logger;

    public MobileLoginModel(AuthService authService, ILogger<MobileLoginModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

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
                _logger.LogInformation("Mobile user logged in: {Email}", Input.Email);
                
                // Verify user is MobileUser
                if (user.UserType != UserType.MobileUser)
                {
                    // User logged in but is not MobileUser, logout and show error
                    await _authService.LogoutAsync();
                    ModelState.AddModelError(string.Empty, "This account is for admin access. Please use the Admin Login page.");
                    return Page();
                }
                
                // Redirect to Mobile Dashboard (user can change password in profile)
                return RedirectToPage("/Mobile/Dashboard");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your email and password.");
                return Page();
            }
        }

        return Page();
    }
}

