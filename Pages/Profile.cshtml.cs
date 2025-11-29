using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyInventory.Models;
using PropertyInventory.Services;

namespace PropertyInventory.Pages;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly AuthService _authService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ProfileModel> _logger;

    public ProfileModel(AuthService authService, IWebHostEnvironment environment, ILogger<ProfileModel> logger)
    {
        _authService = authService;
        _environment = environment;
        _logger = logger;
    }

    public new ApplicationUser? User { get; set; }

    [BindProperty]
    public PasswordInputModel PasswordInput { get; set; } = default!;

    public class PasswordInputModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
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

    public async Task OnGetAsync()
    {
        var userId = _authService.GetCurrentUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            User = await _authService.GetUserByIdAsync(userId);
        }
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        var userId = _authService.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            TempData["Error"] = "Unable to identify user. Please login again.";
            await LoadUserAsync();
            return Page();
        }

        if (ModelState.IsValid)
        {
            var result = await _authService.ChangePasswordAsync(userId, PasswordInput.CurrentPassword, PasswordInput.NewPassword);

            if (result)
            {
                _logger.LogInformation("User {UserId} changed password successfully", userId);
                TempData["Success"] = "Password changed successfully!";
                return RedirectToPage();
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Current password is incorrect. Please try again.");
            }
        }

        await LoadUserAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUploadProfilePictureAsync(IFormFile? profilePicture)
    {
        var userId = _authService.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            TempData["Error"] = "Unable to identify user. Please login again.";
            await LoadUserAsync();
            return Page();
        }

        if (profilePicture == null || profilePicture.Length == 0)
        {
            TempData["Error"] = "Please select a file to upload.";
            await LoadUserAsync();
            return Page();
        }

        // Validate file size (5MB max)
        if (profilePicture.Length > 5 * 1024 * 1024)
        {
            TempData["Error"] = "File size exceeds 5MB limit.";
            await LoadUserAsync();
            return Page();
        }

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var fileExtension = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            TempData["Error"] = "Invalid file type. Only JPG, PNG, and GIF files are allowed.";
            await LoadUserAsync();
            return Page();
        }

        try
        {
            // Create profile-pictures directory if it doesn't exist
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "profile-pictures");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate unique filename
            var fileName = $"{userId.Replace("/", "_")}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Delete old profile picture if exists
            var user = await _authService.GetUserByIdAsync(userId);
            if (user != null && !string.IsNullOrEmpty(user.ProfilePicturePath))
            {
                var oldFilePath = Path.Combine(_environment.WebRootPath, user.ProfilePicturePath.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            // Save new file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await profilePicture.CopyToAsync(fileStream);
            }

            // Update user profile picture path
            var relativePath = $"/profile-pictures/{fileName}";
            await _authService.UpdateUserProfileAsync(userId, relativePath);

            TempData["Success"] = "Profile picture uploaded successfully!";
            _logger.LogInformation("User {UserId} uploaded profile picture", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile picture for user {UserId}", userId);
            TempData["Error"] = "An error occurred while uploading the profile picture. Please try again.";
        }

        await LoadUserAsync();
        return Page();
    }

    private async Task LoadUserAsync()
    {
        var userId = _authService.GetCurrentUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            User = await _authService.GetUserByIdAsync(userId);
        }
    }
}

