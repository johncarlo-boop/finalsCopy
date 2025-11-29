using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyInventory.Models;
using PropertyInventory.Services;
using Microsoft.AspNetCore.SignalR;
using PropertyInventory.Hubs;

namespace PropertyInventory.Pages.Mobile;

[Authorize]
public class BorrowModel : PageModel
{
    private readonly FirebaseService _firebaseService;
    private readonly AuthService _authService;
    private readonly IHubContext<PropertyHub> _hubContext;

    public BorrowModel(FirebaseService firebaseService, AuthService authService, IHubContext<PropertyHub> hubContext)
    {
        _firebaseService = firebaseService;
        _authService = authService;
        _hubContext = hubContext;
    }

    [BindProperty]
    public string PropertyId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Borrower name is required")]
    [Display(Name = "Borrower Name")]
    [BindProperty]
    public string BorrowerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Return date and time is required")]
    [Display(Name = "Return Date & Time")]
    [DataType(DataType.DateTime)]
    [BindProperty]
    public DateTime ReturnDate { get; set; }

    public Property? Property { get; set; }
    public Dictionary<string, int>? QuantityBreakdown { get; set; }
    public Dictionary<string, List<Property>>? PropertiesByStatus { get; set; }
    public List<Property>? IndividualItems { get; set; }

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        // Check if user is a mobile user
        var userType = _authService.GetCurrentUserType();
        if (userType != "MobileUser")
        {
            return RedirectToPage("/Index");
        }

        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        Property = await _firebaseService.GetPropertyByIdAsync(id);
        
        if (Property == null)
        {
            return NotFound();
        }

        PropertyId = Property.Id;
        
        // Get quantity breakdown and individual items by ImageUrl if available, otherwise by PropertyCode
        if (!string.IsNullOrWhiteSpace(Property.ImageUrl))
        {
            QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByImageUrlAsync(Property.ImageUrl);
            IndividualItems = await _firebaseService.GetIndividualItemsByImageUrlAsync(Property.ImageUrl);
        }
        else
        {
            QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByPropertyCodeAsync(Property.PropertyCode);
            IndividualItems = await _firebaseService.GetIndividualItemsByPropertyCodeAsync(Property.PropertyCode);
        }
        
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Check if user is a mobile user
        var userType = _authService.GetCurrentUserType();
        if (userType != "MobileUser")
        {
            return RedirectToPage("/Index");
        }

        if (!ModelState.IsValid)
        {
            Property = await _firebaseService.GetPropertyByIdAsync(PropertyId);
            if (Property != null)
            {
                if (!string.IsNullOrWhiteSpace(Property.ImageUrl))
                {
                    QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByImageUrlAsync(Property.ImageUrl);
                    IndividualItems = await _firebaseService.GetIndividualItemsByImageUrlAsync(Property.ImageUrl);
                }
                else
                {
                    QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByPropertyCodeAsync(Property.PropertyCode);
                    IndividualItems = await _firebaseService.GetIndividualItemsByPropertyCodeAsync(Property.PropertyCode);
                }
            }
            return Page();
        }

        if (string.IsNullOrEmpty(PropertyId))
        {
            TempData["ErrorMessage"] = "Property ID is required.";
            return RedirectToPage("/Mobile/Dashboard");
        }

        try
        {
            Property = await _firebaseService.GetPropertyByIdAsync(PropertyId);
            
            if (Property == null)
            {
                TempData["ErrorMessage"] = "Property not found.";
                return RedirectToPage("/Mobile/Dashboard");
            }

            // Update property with borrowing information
            Property.BorrowerName = BorrowerName;
            Property.BorrowedDate = DateTime.UtcNow; // Record when property was borrowed (date and time)
            Property.ReturnDate = ReturnDate.ToUniversalTime(); // Return date and time
            Property.Status = PropertyStatus.InUse;
            Property.OverdueNotificationSent = false; // Reset notification flag for new borrowing
            Property.LastUpdated = DateTime.UtcNow;
            Property.UpdatedBy = _authService.GetCurrentUserEmail() ?? "Unknown";

            await _firebaseService.UpdatePropertyAsync(PropertyId, Property);
            
            // Notify all clients via SignalR with borrower name
            await _hubContext.Clients.All.SendAsync("PropertyUpdated", Property.PropertyCode, "borrowed", BorrowerName);
            
            TempData["SuccessMessage"] = $"Property {Property.PropertyCode} has been borrowed by {BorrowerName}.";
            return RedirectToPage("/Mobile/ScanResult", new { propertyCode = Property.PropertyCode });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"An error occurred while borrowing the property: {ex.Message}";
            Property = await _firebaseService.GetPropertyByIdAsync(PropertyId);
            if (Property == null)
            {
                return RedirectToPage("/Mobile/Dashboard");
            }
            if (!string.IsNullOrWhiteSpace(Property.ImageUrl))
            {
                QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByImageUrlAsync(Property.ImageUrl);
                IndividualItems = await _firebaseService.GetIndividualItemsByImageUrlAsync(Property.ImageUrl);
            }
            else
            {
                QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByPropertyCodeAsync(Property.PropertyCode);
                IndividualItems = await _firebaseService.GetIndividualItemsByPropertyCodeAsync(Property.PropertyCode);
            }
            return Page();
        }
    }

    public async Task<IActionResult> OnPostBorrowAsync(string? propertyId)
    {
        // Check if user is a mobile user
        var userType = _authService.GetCurrentUserType();
        if (userType != "MobileUser")
        {
            return RedirectToPage("/Index");
        }

        if (string.IsNullOrEmpty(propertyId))
        {
            TempData["ErrorMessage"] = "Property ID is required.";
            return RedirectToPage("/Mobile/Borrow", new { id = PropertyId });
        }

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(BorrowerName) || ReturnDate == default)
        {
            TempData["ErrorMessage"] = "Borrower name and return date are required.";
            return RedirectToPage("/Mobile/Borrow", new { id = PropertyId });
        }

        try
        {
            var property = await _firebaseService.GetPropertyByIdAsync(propertyId);
            if (property == null)
            {
                TempData["ErrorMessage"] = "Property not found.";
                return RedirectToPage("/Mobile/Dashboard");
            }

            // Update property with borrowing information
            property.BorrowerName = BorrowerName;
            property.BorrowedDate = DateTime.UtcNow;
            property.ReturnDate = ReturnDate.ToUniversalTime();
            property.Status = PropertyStatus.InUse;
            property.OverdueNotificationSent = false;
            property.LastUpdated = DateTime.UtcNow;
            property.UpdatedBy = _authService.GetCurrentUserEmail() ?? "Unknown";

            await _firebaseService.UpdatePropertyAsync(propertyId, property);

            // Notify all clients via SignalR
            await _hubContext.Clients.All.SendAsync("PropertyUpdated", property.PropertyCode, "borrowed", BorrowerName);

            TempData["SuccessMessage"] = $"Property {property.PropertyCode} (Tag: {property.SerialNumber}) has been borrowed by {BorrowerName}.";
            return RedirectToPage("/Mobile/Borrow", new { id = PropertyId });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"An error occurred while borrowing the property: {ex.Message}";
            return RedirectToPage("/Mobile/Borrow", new { id = PropertyId });
        }
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
            TempData["ErrorMessage"] = "Property ID is required.";
            return RedirectToPage("/Mobile/Borrow", new { id = PropertyId });
        }

        try
        {
            var property = await _firebaseService.GetPropertyByIdAsync(propertyId);
            if (property == null)
            {
                TempData["ErrorMessage"] = "Property not found.";
                return RedirectToPage("/Mobile/Dashboard");
            }

            // Clear borrowing information
            property.BorrowerName = null;
            property.BorrowedDate = null;
            property.ReturnDate = null;
            property.Status = PropertyStatus.Available;
            property.OverdueNotificationSent = false;
            property.LastUpdated = DateTime.UtcNow;
            property.UpdatedBy = _authService.GetCurrentUserEmail() ?? "Unknown";

            await _firebaseService.UpdatePropertyAsync(propertyId, property);

            // Notify all clients via SignalR
            await _hubContext.Clients.All.SendAsync("PropertyUpdated", property.PropertyCode, "returned", null);

            TempData["SuccessMessage"] = $"Property {property.PropertyCode} (Tag: {property.SerialNumber}) has been returned.";
            return RedirectToPage("/Mobile/Borrow", new { id = PropertyId });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"An error occurred while returning the property: {ex.Message}";
            return RedirectToPage("/Mobile/Borrow", new { id = PropertyId });
        }
    }
}

