using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using PropertyInventory.Hubs;
using PropertyInventory.Models;
using PropertyInventory.Services;

namespace PropertyInventory.Pages.Mobile;

[Authorize]
public class ScanResultModel : PageModel
{
    private readonly FirebaseService _firebaseService;
    private readonly AuthService _authService;
    private readonly IHubContext<PropertyHub> _hubContext;

    public ScanResultModel(FirebaseService firebaseService, AuthService authService, IHubContext<PropertyHub> hubContext)
    {
        _firebaseService = firebaseService;
        _authService = authService;
        _hubContext = hubContext;
    }

    public Property? Property { get; set; }
    public Dictionary<string, int>? QuantityBreakdown { get; set; }
    
    [TempData]
    public string? ErrorMessage { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [BindProperty]
    public string PropertyId { get; set; } = string.Empty;

    public bool CanReturn => Property != null && Property.Status == PropertyStatus.InUse && !string.IsNullOrEmpty(Property.BorrowerName);

    public async Task<IActionResult> OnGetAsync(string? propertyCode)
    {
        // Check if user is a mobile user
        var userType = _authService.GetCurrentUserType();
        if (userType != "MobileUser")
        {
            return RedirectToPage("/Index");
        }

        if (string.IsNullOrEmpty(propertyCode))
        {
            ErrorMessage = "Walang QR data na nakuha. Pakisubukan ulit ang pag-scan.";
            return Page();
        }

        Property = await _firebaseService.GetPropertyByCodeAsync(propertyCode);

        if (Property == null)
        {
            ErrorMessage = $"Property na may code na '{propertyCode}' ay hindi nahanap.";
            return Page();
        }

        PropertyId = Property.Id;

        // Get quantity breakdown by ImageUrl if available, otherwise by PropertyCode
        if (!string.IsNullOrWhiteSpace(Property.ImageUrl))
        {
            QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByImageUrlAsync(Property.ImageUrl);
        }
        else
        {
            QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByPropertyCodeAsync(Property.PropertyCode);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostReturnAsync(string? propertyId)
    {
        // Check if user is a mobile user
        var userType = _authService.GetCurrentUserType();
        if (userType != "MobileUser")
        {
            return RedirectToPage("/Index");
        }

        if (string.IsNullOrEmpty(propertyId))
        {
            ErrorMessage = "Property ID is required to return the item.";
            return RedirectToPage("/Mobile/Dashboard");
        }

        var property = await _firebaseService.GetPropertyByIdAsync(propertyId);
        if (property == null)
        {
            ErrorMessage = "Property not found.";
            return RedirectToPage("/Mobile/Dashboard");
        }

        // Reset borrowing details
        property.Status = PropertyStatus.Available;
        property.BorrowerName = null;
        property.BorrowedDate = null;
        property.ReturnDate = null;
        property.OverdueNotificationSent = false;
        property.LastUpdated = DateTime.UtcNow;
        property.UpdatedBy = _authService.GetCurrentUserEmail() ?? "Unknown";

        await _firebaseService.UpdatePropertyAsync(property.Id, property);

        // Notify all clients for real-time updates
        await _hubContext.Clients.All.SendAsync("PropertyUpdated", property.PropertyCode, "returned", null);

        SuccessMessage = $"Property {property.PropertyCode} has been marked as returned.";
        return RedirectToPage("/Mobile/ScanResult", new { propertyCode = property.PropertyCode });
    }
}

