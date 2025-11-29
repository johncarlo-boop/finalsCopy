using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyInventory.Models;
using PropertyInventory.Services;

namespace PropertyInventory.Pages.Mobile;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly FirebaseService _firebaseService;
    private readonly AuthService _authService;
    private readonly ILogger<ProfileModel> _logger;

    public ProfileModel(FirebaseService firebaseService, AuthService authService, ILogger<ProfileModel> logger)
    {
        _firebaseService = firebaseService;
        _authService = authService;
        _logger = logger;
    }

    public ApplicationUser? CurrentUser { get; set; }

    [BindProperty]
    public ChangePasswordInputModel ChangePasswordInput { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class ChangePasswordInputModel
    {
        [Required(ErrorMessage = "Current password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your new password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        // Check if user is a mobile user
        var userType = _authService.GetCurrentUserType();
        if (userType != "MobileUser")
        {
            return RedirectToPage("/Index");
        }

        // Get current user
        var userId = _authService.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            ErrorMessage = "User not found.";
            return RedirectToPage("/Mobile/Dashboard");
        }

        CurrentUser = await _authService.GetUserByIdAsync(userId);
        if (CurrentUser == null)
        {
            ErrorMessage = "User not found.";
            return RedirectToPage("/Mobile/Dashboard");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        // Check if user is a mobile user
        var userType = _authService.GetCurrentUserType();
        if (userType != "MobileUser")
        {
            return RedirectToPage("/Index");
        }

        if (!ModelState.IsValid)
        {
            // Reload user data
            var userId = _authService.GetCurrentUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                CurrentUser = await _authService.GetUserByIdAsync(userId);
            }
            return Page();
        }

        try
        {
            var userId = _authService.GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                ErrorMessage = "User not found.";
                return RedirectToPage("/Mobile/Dashboard");
            }

            // Verify current password
            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                ErrorMessage = "User not found.";
                return RedirectToPage("/Mobile/Dashboard");
            }

            // Check if current password is correct
            if (!BCrypt.Net.BCrypt.Verify(ChangePasswordInput.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("ChangePasswordInput.CurrentPassword", "Current password is incorrect.");
                CurrentUser = user;
                return Page();
            }

            // Update password
            var result = await _authService.ChangePasswordAsync(userId, ChangePasswordInput.CurrentPassword, ChangePasswordInput.NewPassword);

            if (result)
            {
                SuccessMessage = "Password changed successfully!";
                _logger.LogInformation("User {UserId} changed password successfully", userId);
                return RedirectToPage("/Mobile/Profile");
            }
            else
            {
                ErrorMessage = "Failed to change password. Please try again.";
                CurrentUser = user;
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            ErrorMessage = "An error occurred while changing your password. Please try again.";
            
            // Reload user data
            var userId = _authService.GetCurrentUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                CurrentUser = await _authService.GetUserByIdAsync(userId);
            }
            return Page();
        }
    }
}

