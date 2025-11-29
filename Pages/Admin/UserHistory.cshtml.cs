using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyInventory.Models;
using PropertyInventory.Services;

namespace PropertyInventory.Pages.Admin;

[Authorize(Roles = "Admin")]
public class UserHistoryModel : PageModel
{
    private readonly FirebaseService _firebaseService;
    private readonly AuthService _authService;
    private readonly ILogger<UserHistoryModel> _logger;

    public UserHistoryModel(FirebaseService firebaseService, AuthService authService, ILogger<UserHistoryModel> logger)
    {
        _firebaseService = firebaseService;
        _authService = authService;
        _logger = logger;
    }

    public new ApplicationUser? User { get; set; }
    public List<Property> EditedProperties { get; set; } = new();
    public List<Property> BorrowingHistory { get; set; } = new();
    public int TotalEdits => EditedProperties.Count;
    public int TotalBorrows => BorrowingHistory.Count;
    public int ActiveBorrows => BorrowingHistory.Count(p => p.Status == PropertyStatus.InUse);

    [BindProperty]
    public EditUserInputModel EditUserInput { get; set; } = new();

    [BindProperty]
    public ChangeUserPasswordInputModel ChangePasswordInput { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class EditUserInputModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }

    public class ChangeUserPasswordInputModel
    {
        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm the new password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return NotFound();
        }

        User = await _firebaseService.GetUserByIdAsync(userId);
        
        if (User == null)
        {
            return NotFound();
        }

        // Initialize edit input with current user data
        EditUserInput = new EditUserInputModel
        {
            FullName = User.FullName ?? string.Empty,
            Email = User.Email ?? string.Empty
        };

        // Get user's edit history
        if (!string.IsNullOrEmpty(User.Email))
        {
            EditedProperties = await _firebaseService.GetPropertiesEditedByUserAsync(User.Email);
            BorrowingHistory = await _firebaseService.GetBorrowingHistoryByUserAsync(User.Email);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateUserAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            ErrorMessage = "User ID is required.";
            return RedirectToPage();
        }

        if (!ModelState.IsValid)
        {
            // Reload user data
            User = await _firebaseService.GetUserByIdAsync(userId);
            if (User != null && !string.IsNullOrEmpty(User.Email))
            {
                EditedProperties = await _firebaseService.GetPropertiesEditedByUserAsync(User.Email);
                BorrowingHistory = await _firebaseService.GetBorrowingHistoryByUserAsync(User.Email);
            }
            return Page();
        }

        try
        {
            var user = await _firebaseService.GetUserByIdAsync(userId);
            if (user == null)
            {
                ErrorMessage = "User not found.";
                return RedirectToPage();
            }

            // Update user information
            user.FullName = EditUserInput.FullName;
            user.Email = EditUserInput.Email;

            await _firebaseService.UpdateUserAsync(userId, user);

            SuccessMessage = "User information updated successfully.";
            _logger.LogInformation("Admin updated user {UserId} information", userId);
            return RedirectToPage(new { userId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            ErrorMessage = "An error occurred while updating user information. Please try again.";
            return RedirectToPage(new { userId });
        }
    }

    public async Task<IActionResult> OnPostChangePasswordAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            ErrorMessage = "User ID is required.";
            return RedirectToPage();
        }

        if (!ModelState.IsValid)
        {
            // Reload user data
            User = await _firebaseService.GetUserByIdAsync(userId);
            if (User != null && !string.IsNullOrEmpty(User.Email))
            {
                EditedProperties = await _firebaseService.GetPropertiesEditedByUserAsync(User.Email);
                BorrowingHistory = await _firebaseService.GetBorrowingHistoryByUserAsync(User.Email);
            }
            return Page();
        }

        try
        {
            var user = await _firebaseService.GetUserByIdAsync(userId);
            if (user == null)
            {
                ErrorMessage = "User not found.";
                return RedirectToPage();
            }

            // Update password directly (admin can reset password without current password)
            var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(ChangePasswordInput.NewPassword);
            user.PasswordHash = newPasswordHash;
            user.RequiresPasswordChange = false; // User can login with new password

            await _firebaseService.UpdateUserAsync(userId, user);

            SuccessMessage = "User password changed successfully.";
            _logger.LogInformation("Admin changed password for user {UserId}", userId);
            return RedirectToPage(new { userId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            ErrorMessage = "An error occurred while changing the password. Please try again.";
            return RedirectToPage(new { userId });
        }
    }
}

