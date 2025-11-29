using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyInventory.Hubs;
using PropertyInventory.Models;
using PropertyInventory.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace PropertyInventory.Pages;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly FirebaseService _firebaseService;
    private readonly IHubContext<PropertyHub> _hubContext;
    private readonly AuthService _authService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(FirebaseService firebaseService, IHubContext<PropertyHub> hubContext, AuthService authService, ILogger<IndexModel> logger)
    {
        _firebaseService = firebaseService;
        _hubContext = hubContext;
        _authService = authService;
        _logger = logger;
    }

    public IList<Property> Properties { get; set; } = default!;
    public IList<Property> BorrowedProperties { get; set; } = default!;
    public List<ApplicationUser> ActiveUsers { get; set; } = default!;
    public List<string> Categories { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public string? SearchString { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? CategoryFilter { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public PropertyStatus? StatusFilter { get; set; }

    public async Task OnGetAsync()
    {
        // Redirect mobile users to their dashboard
        var userType = _authService.GetCurrentUserType();
        if (userType == "MobileUser")
        {
            Response.Redirect("/Mobile/Dashboard");
            return;
        }

        Categories = await _firebaseService.GetAllCategoriesAsync();
        Properties = await _firebaseService.GetGroupedPropertiesAsync(SearchString, CategoryFilter, StatusFilter);
        BorrowedProperties = await _firebaseService.GetBorrowedPropertiesAsync();
        ActiveUsers = await _firebaseService.GetAllActiveUsersAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        try
        {
            var property = await _firebaseService.GetPropertyByIdAsync(id);
            
            if (property == null)
            {
                TempData["ErrorMessage"] = "Property not found.";
                return RedirectToPage("./Index");
            }

            var propertyCode = property.PropertyCode;
            await _firebaseService.DeletePropertyAsync(id);

            // Notify all clients via SignalR
            await _hubContext.Clients.All.SendAsync("PropertyDeleted", propertyCode);

            TempData["SuccessMessage"] = $"Property {propertyCode} has been deleted successfully.";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"An error occurred while deleting the property: {ex.Message}";
            return RedirectToPage("./Index");
        }
    }

    public async Task<IActionResult> OnPostDeleteUserAsync(string userId)
    {
        try
        {
            var user = await _firebaseService.GetUserByIdAsync(userId);
            
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToPage("./Index");
            }

            // Prevent deleting admin users
            if (user.IsAdmin || user.UserType == UserType.Admin)
            {
                TempData["ErrorMessage"] = "Cannot delete admin users.";
                return RedirectToPage("./Index");
            }

            var userName = user.FullName ?? user.Email;
            var userEmail = user.Email;
            var deleted = await _firebaseService.DeleteUserAsync(userId);

            if (!deleted)
            {
                TempData["ErrorMessage"] = $"Failed to delete user {userName}.";
                return RedirectToPage("./Index");
            }

            // Also delete associated account request if exists (to update approved count)
            try
            {
                var allRequests = await _firebaseService.GetAllAccountRequestsAsync();
                var associatedRequest = allRequests.FirstOrDefault(r => 
                    r.Email.Equals(userEmail, StringComparison.OrdinalIgnoreCase) && 
                    r.Status == AccountRequestStatus.Approved);
                
                if (associatedRequest != null && !string.IsNullOrEmpty(associatedRequest.Id))
                {
                    await _firebaseService.DeleteAccountRequestAsync(associatedRequest.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error deleting associated account request for user {Email}", userEmail);
                // Continue even if account request deletion fails
            }

            // Send SignalR notification for user deletion
            await _hubContext.Clients.All.SendAsync("UserDeleted", new
            {
                userId,
                userName,
                userEmail,
                deletedAt = DateTime.UtcNow
            });

            TempData["SuccessMessage"] = $"User {userName} has been removed successfully. They can no longer access the system.";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"An error occurred while removing the user: {ex.Message}";
            return RedirectToPage("./Index");
        }
    }


    public async Task<IActionResult> OnGetPropertyItemsAsync(string imageUrl)
    {
        try
        {
            var items = await _firebaseService.GetPropertiesByImageUrlAsync(imageUrl);
            return new JsonResult(items.Select(p => new {
                id = p.Id,
                propertyCode = p.PropertyCode,
                serialNumber = p.SerialNumber,
                status = p.Status.ToString()
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting property items");
            return new JsonResult(new List<object>());
        }
    }

    public async Task<IActionResult> OnPostDeleteMultipleAsync(List<string> selectedIds)
    {
        try
        {
            if (selectedIds == null || !selectedIds.Any())
            {
                TempData["ErrorMessage"] = "No items selected for deletion.";
                return RedirectToPage("./Index");
            }

            var success = await _firebaseService.DeletePropertiesAsync(selectedIds);

            if (success)
            {
                // Notify clients
                // Note: We might want to send a more specific update, but refreshing the list works too
                await _hubContext.Clients.All.SendAsync("PropertiesDeleted", selectedIds);
                
                TempData["SuccessMessage"] = $"Successfully deleted {selectedIds.Count} items.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete selected items.";
            }

            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"An error occurred while deleting items: {ex.Message}";
            return RedirectToPage("./Index");
        }
    }
}

