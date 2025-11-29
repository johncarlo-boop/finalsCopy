using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyInventory.Models;
using PropertyInventory.Services;
using System.Text.Json;

namespace PropertyInventory.Pages.Mobile;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly FirebaseService _firebaseService;
    private readonly AuthService _authService;
    private readonly ILogger<DashboardModel> _logger;

    public DashboardModel(FirebaseService firebaseService, AuthService authService, ILogger<DashboardModel> logger)
    {
        _firebaseService = firebaseService;
        _authService = authService;
        _logger = logger;
    }

    public List<Property> EditedProperties { get; set; } = new();
    public List<Property> BorrowingHistory { get; set; } = new();
    public ApplicationUser? CurrentUser { get; set; }

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
        if (!string.IsNullOrEmpty(userId))
        {
            CurrentUser = await _authService.GetUserByIdAsync(userId);
        }

        // Get properties edited by this user
        var userEmail = _authService.GetCurrentUserEmail();
        if (!string.IsNullOrEmpty(userEmail))
        {
            EditedProperties = await _firebaseService.GetPropertiesEditedByUserAsync(userEmail);
            BorrowingHistory = await _firebaseService.GetBorrowingHistoryByUserAsync(userEmail);
        }

        return Page();
    }

    public async Task<IActionResult> OnGetGetPropertyAsync(string propertyCode)
    {
        try
        {
            var property = await _firebaseService.GetPropertyByCodeAsync(propertyCode);
            
            if (property == null)
            {
                return new JsonResult(new { success = false, message = "Property not found" });
            }

            return new JsonResult(new
            {
                success = true,
                property = new
                {
                    id = property.Id,
                    propertyCode = property.PropertyCode,
                    propertyName = property.PropertyName,
                    category = property.Category,
                    location = property.Location,
                    status = property.Status.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting property: {Code}", propertyCode);
            return new JsonResult(new { success = false, message = "Error fetching property" });
        }
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await _authService.LogoutAsync();
        return RedirectToPage("/Account/MobileLogin");
    }
}





