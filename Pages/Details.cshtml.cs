using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyInventory.Models;
using PropertyInventory.Services;
using Microsoft.AspNetCore.SignalR;
using PropertyInventory.Hubs;
using System.ComponentModel.DataAnnotations;

namespace PropertyInventory.Pages;

[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly FirebaseService _firebaseService;
    private readonly IHubContext<PropertyHub> _hubContext;

    public DetailsModel(FirebaseService firebaseService, IHubContext<PropertyHub> hubContext)
    {
        _firebaseService = firebaseService;
        _hubContext = hubContext;
    }

    public Property? Property { get; set; }
    public Dictionary<string, int>? QuantityBreakdown { get; set; }
    public List<Property>? IndividualItems { get; set; }

    [BindProperty]
    public string? BorrowPropertyId { get; set; }

    [Required(ErrorMessage = "Borrower name is required")]
    [Display(Name = "Borrower Name")]
    [BindProperty]
    public string BorrowerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Return date and time is required")]
    [Display(Name = "Return Date & Time")]
    [DataType(DataType.DateTime)]
    [BindProperty]
    public DateTime? ReturnDate { get; set; }

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        Property = await _firebaseService.GetPropertyByIdAsync(id);
        if (Property == null)
        {
            return NotFound();
        }
        
        // Always get individual items - if ImageUrl exists, get all items with same ImageUrl
        // Otherwise, get items with same PropertyCode
        if (!string.IsNullOrWhiteSpace(Property.ImageUrl))
        {
            IndividualItems = await _firebaseService.GetIndividualItemsByImageUrlAsync(Property.ImageUrl);
            QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByImageUrlAsync(Property.ImageUrl);
            
            // If there are multiple items, use the first one as the representative property
            if (IndividualItems != null && IndividualItems.Count > 0)
            {
                Property = IndividualItems.First(); // Use first item for display
            }
        }
        else
        {
            IndividualItems = await _firebaseService.GetIndividualItemsByPropertyCodeAsync(Property.PropertyCode);
            QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByPropertyCodeAsync(Property.PropertyCode);
        }
        
        // Ensure QuantityBreakdown is always initialized
        if (QuantityBreakdown == null || QuantityBreakdown.Count == 0)
        {
            QuantityBreakdown = new Dictionary<string, int>
            {
                { "Total", Property.Quantity },
                { "Available", Property.Status == PropertyStatus.Available ? Property.Quantity : 0 },
                { "InUse", Property.Status == PropertyStatus.InUse ? Property.Quantity : 0 },
                { "UnderMaintenance", Property.Status == PropertyStatus.UnderMaintenance ? Property.Quantity : 0 },
                { "Damaged", Property.Status == PropertyStatus.Damaged ? Property.Quantity : 0 }
            };
        }
        
        return Page();
    }

    public async Task<IActionResult> OnPostBorrowAsync(string? propertyId)
    {
        if (string.IsNullOrEmpty(propertyId))
        {
            TempData["ErrorMessage"] = "Property ID is required.";
            return RedirectToPage("./Index");
        }

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(BorrowerName) || !ReturnDate.HasValue)
        {
            TempData["ErrorMessage"] = "Borrower name and return date are required.";
            return RedirectToPage("./Details", new { id = propertyId });
        }

        try
        {
            var property = await _firebaseService.GetPropertyByIdAsync(propertyId);
            if (property == null)
            {
                TempData["ErrorMessage"] = "Property not found.";
                return RedirectToPage("./Index");
            }

            // Update property with borrowing information
            property.BorrowerName = BorrowerName;
            property.BorrowedDate = DateTime.UtcNow;
            property.ReturnDate = ReturnDate.Value.ToUniversalTime();
            property.Status = PropertyStatus.InUse;
            property.OverdueNotificationSent = false;
            property.LastUpdated = DateTime.UtcNow;
            property.UpdatedBy = User.Identity?.Name ?? "Unknown";

            await _firebaseService.UpdatePropertyAsync(propertyId, property);

            // Notify all clients via SignalR
            await _hubContext.Clients.All.SendAsync("PropertyUpdated", property.PropertyCode, "borrowed", BorrowerName);

            TempData["SuccessMessage"] = $"Property {property.PropertyCode} (Tag: {property.SerialNumber}) has been borrowed by {BorrowerName}.";
            return RedirectToPage("./Details", new { id = propertyId });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"An error occurred while borrowing the property: {ex.Message}";
            return RedirectToPage("./Details", new { id = propertyId });
        }
    }

    public async Task<IActionResult> OnPostReturnAsync(string? propertyId)
    {
        if (string.IsNullOrEmpty(propertyId))
        {
            TempData["ErrorMessage"] = "Property ID is required.";
            return RedirectToPage("./Index");
        }

        try
        {
            var property = await _firebaseService.GetPropertyByIdAsync(propertyId);
            if (property == null)
            {
                TempData["ErrorMessage"] = "Property not found.";
                return RedirectToPage("./Index");
            }

            // Reset borrowing details
            property.Status = PropertyStatus.Available;
            property.BorrowerName = null;
            property.BorrowedDate = null;
            property.ReturnDate = null;
            property.OverdueNotificationSent = false;
            property.LastUpdated = DateTime.UtcNow;
            property.UpdatedBy = User.Identity?.Name ?? "Unknown";

            await _firebaseService.UpdatePropertyAsync(propertyId, property);

            // Notify all clients via SignalR
            await _hubContext.Clients.All.SendAsync("PropertyUpdated", property.PropertyCode, "returned", null);

            TempData["SuccessMessage"] = $"Property {property.PropertyCode} (Tag: {property.SerialNumber}) has been marked as returned.";
            return RedirectToPage("./Details", new { id = propertyId });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"An error occurred while returning the property: {ex.Message}";
            return RedirectToPage("./Details", new { id = propertyId });
        }
    }

    public async Task<IActionResult> OnPostUpdateQuantityAsync(string? propertyId, int quantity)
    {
        if (string.IsNullOrEmpty(propertyId))
        {
            TempData["ErrorMessage"] = "Property ID is required.";
            return RedirectToPage("./Index");
        }

        if (quantity < 1)
        {
            TempData["ErrorMessage"] = "Quantity must be at least 1.";
            return RedirectToPage("./Details", new { id = propertyId });
        }

        try
        {
            var property = await _firebaseService.GetPropertyByIdAsync(propertyId);
            if (property == null)
            {
                TempData["ErrorMessage"] = "Property not found.";
                return RedirectToPage("./Index");
            }

            // Update quantity
            property.Quantity = quantity;
            property.LastUpdated = DateTime.UtcNow;
            property.UpdatedBy = User.Identity?.Name ?? "Unknown";

            await _firebaseService.UpdatePropertyAsync(propertyId, property);

            // Notify all clients via SignalR
            await _hubContext.Clients.All.SendAsync("PropertyUpdated", property.PropertyCode, "quantity updated", null);

            TempData["SuccessMessage"] = $"Quantity for property {property.PropertyCode} has been updated to {quantity}.";
            return RedirectToPage("./Details", new { id = propertyId });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"An error occurred while updating quantity: {ex.Message}";
            return RedirectToPage("./Details", new { id = propertyId });
        }
    }
}

