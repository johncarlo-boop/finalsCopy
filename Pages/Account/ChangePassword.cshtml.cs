using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyInventory.Services;

namespace PropertyInventory.Pages.Account;

[Authorize(Roles = "Admin")]
public class ChangePasswordModel : PageModel
{
    private readonly AuthService _authService;
    private readonly ILogger<ChangePasswordModel> _logger;

    public ChangePasswordModel(AuthService authService, ILogger<ChangePasswordModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public class InputModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password (Temporary Password)")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet()
    {
        // Check if password change is required
        if (!_authService.RequiresPasswordChange())
        {
            // If not required, redirect to home
            Response.Redirect("/");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!_authService.RequiresPasswordChange())
        {
            return RedirectToPage("/");
        }

        if (ModelState.IsValid)
        {
            var userId = _authService.GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Unable to identify user. Please login again.";
                return Page();
            }

            var result = await _authService.ChangePasswordAsync(userId, Input.CurrentPassword, Input.NewPassword);

            if (result)
            {
                _logger.LogInformation("User {UserId} changed password successfully", userId);
                
                // Logout the user after password change
                await _authService.LogoutAsync();
                
                TempData["PasswordChanged"] = "Password changed successfully! Please login with your new password.";
                return RedirectToPage("./Login");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Current password is incorrect. Please try again.");
            }
        }

        return Page();
    }
}

