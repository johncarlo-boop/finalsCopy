using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using PropertyInventory.Hubs;
using PropertyInventory.Models;
using PropertyInventory.Services;

namespace PropertyInventory.Pages.Mobile;

[Authorize]
public class EditPropertyModel : PageModel
{
    private readonly FirebaseService _firebaseService;
    private readonly AuthService _authService;
    private readonly ILogger<EditPropertyModel> _logger;
    private readonly IHubContext<PropertyHub> _hubContext;

    public EditPropertyModel(FirebaseService firebaseService, AuthService authService, ILogger<EditPropertyModel> logger, IHubContext<PropertyHub> hubContext)
    {
        _firebaseService = firebaseService;
        _authService = authService;
        _logger = logger;
        _hubContext = hubContext;
    }

    [BindProperty]
    public Property? Property { get; set; }

    public string? ErrorMessage { get; set; }
    public List<Property>? IndividualItems { get; set; }
    public Dictionary<string, int>? QuantityBreakdown { get; set; }

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
            ErrorMessage = "Property ID is required.";
            return Page();
        }

        Property = await _firebaseService.GetPropertyByIdAsync(id);
        
        if (Property == null)
        {
            ErrorMessage = "Property not found.";
            return Page();
        }

        // Ensure IndividualItems and QuantityBreakdown are always initialized
        try
        {
            // Get individual items and quantity breakdown
            if (!string.IsNullOrWhiteSpace(Property.ImageUrl))
            {
                IndividualItems = await _firebaseService.GetIndividualItemsByImageUrlAsync(Property.ImageUrl);
                QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByImageUrlAsync(Property.ImageUrl);
            }
            else if (!string.IsNullOrWhiteSpace(Property.PropertyCode))
            {
                IndividualItems = await _firebaseService.GetIndividualItemsByPropertyCodeAsync(Property.PropertyCode);
                QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByPropertyCodeAsync(Property.PropertyCode);
            }
            else
            {
                // Fallback: initialize with current property
                IndividualItems = new List<Property> { Property };
                QuantityBreakdown = new Dictionary<string, int>
                {
                    { "Total", 1 },
                    { "Available", Property.Status == PropertyStatus.Available ? 1 : 0 },
                    { "InUse", Property.Status == PropertyStatus.InUse ? 1 : 0 },
                    { "UnderMaintenance", Property.Status == PropertyStatus.UnderMaintenance ? 1 : 0 },
                    { "Damaged", Property.Status == PropertyStatus.Damaged ? 1 : 0 }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading individual items in OnGetAsync: {Id}", id);
            // Fallback: initialize with current property
            IndividualItems = new List<Property> { Property };
            QuantityBreakdown = new Dictionary<string, int>
            {
                { "Total", 1 },
                { "Available", Property.Status == PropertyStatus.Available ? 1 : 0 },
                { "InUse", Property.Status == PropertyStatus.InUse ? 1 : 0 },
                { "UnderMaintenance", Property.Status == PropertyStatus.UnderMaintenance ? 1 : 0 },
                { "Damaged", Property.Status == PropertyStatus.Damaged ? 1 : 0 }
            };
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

        try
        {
            if (Property == null)
            {
                _logger.LogWarning("Property is null in OnPostAsync");
                TempData["ErrorMessage"] = "Invalid property data. Please try again.";
                return RedirectToPage("/Mobile/Dashboard");
            }

            if (string.IsNullOrEmpty(Property.Id))
            {
                _logger.LogWarning("Property.Id is null or empty in OnPostAsync");
                TempData["ErrorMessage"] = "Invalid property data. Please try again.";
                return RedirectToPage("/Mobile/Dashboard");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid. Errors: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                
                // Reload property data for display
                Property = await _firebaseService.GetPropertyByIdAsync(Property.Id);
                if (Property == null)
                {
                    TempData["ErrorMessage"] = "Property not found.";
                    return RedirectToPage("/Mobile/Dashboard");
                }
                
                // Always get individual items and quantity breakdown
                try
                {
                    if (!string.IsNullOrWhiteSpace(Property.ImageUrl))
                    {
                        IndividualItems = await _firebaseService.GetIndividualItemsByImageUrlAsync(Property.ImageUrl);
                        QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByImageUrlAsync(Property.ImageUrl);
                    }
                    else if (!string.IsNullOrWhiteSpace(Property.PropertyCode))
                    {
                        IndividualItems = await _firebaseService.GetIndividualItemsByPropertyCodeAsync(Property.PropertyCode);
                        QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByPropertyCodeAsync(Property.PropertyCode);
                    }
                    else
                    {
                        // Fallback: initialize empty lists if no grouping criteria
                        IndividualItems = new List<Property> { Property };
                        QuantityBreakdown = new Dictionary<string, int>
                        {
                            { "Total", 1 },
                            { "Available", Property.Status == PropertyStatus.Available ? 1 : 0 },
                            { "InUse", Property.Status == PropertyStatus.InUse ? 1 : 0 },
                            { "UnderMaintenance", Property.Status == PropertyStatus.UnderMaintenance ? 1 : 0 },
                            { "Damaged", Property.Status == PropertyStatus.Damaged ? 1 : 0 }
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading individual items in ModelState invalid: {Id}", Property.Id);
                    // Fallback: initialize with current property
                    IndividualItems = new List<Property> { Property };
                    QuantityBreakdown = new Dictionary<string, int>
                    {
                        { "Total", 1 },
                        { "Available", Property.Status == PropertyStatus.Available ? 1 : 0 },
                        { "InUse", Property.Status == PropertyStatus.InUse ? 1 : 0 },
                        { "UnderMaintenance", Property.Status == PropertyStatus.UnderMaintenance ? 1 : 0 },
                        { "Damaged", Property.Status == PropertyStatus.Damaged ? 1 : 0 }
                    };
                }
                return Page();
            }

            // Get existing property to preserve other fields
            var existingProperty = await _firebaseService.GetPropertyByIdAsync(Property.Id);
            if (existingProperty == null)
            {
                TempData["ErrorMessage"] = "Property not found.";
                return RedirectToPage("/Mobile/Dashboard");
            }

            // Check if property is currently borrowed - prevent editing if borrowed
            if (existingProperty.Status == PropertyStatus.InUse && !string.IsNullOrWhiteSpace(existingProperty.BorrowerName))
            {
                TempData["ErrorMessage"] = $"This property is currently borrowed by {existingProperty.BorrowerName}. Please return it first before editing.";
                // Redirect to GET to ensure clean state
                return RedirectToPage("/Mobile/EditProperty", new { id = Property.Id });
            }

            // Store original status before updating
            var originalStatus = existingProperty.Status;
            var newStatus = Property.Status;

            // If status changed from InUse to something else, clear borrower info and reset notification flag
            if (originalStatus == PropertyStatus.InUse && newStatus != PropertyStatus.InUse)
            {
                existingProperty.BorrowerName = null;
                existingProperty.BorrowedDate = null;
                existingProperty.ReturnDate = null;
                existingProperty.OverdueNotificationSent = false;
            }
            // If status changed to InUse, ensure borrower info is preserved if it exists
            else if (originalStatus != PropertyStatus.InUse && newStatus == PropertyStatus.InUse)
            {
                // Don't clear borrower info if it already exists (might be set from borrow action)
                // Just ensure OverdueNotificationSent is reset
                existingProperty.OverdueNotificationSent = false;
            }

            // Update only allowed fields for mobile users
            existingProperty.Location = Property.Location ?? existingProperty.Location ?? string.Empty;
            existingProperty.Status = Property.Status;
            existingProperty.Remarks = Property.Remarks;
            existingProperty.LastUpdated = DateTime.UtcNow;
            existingProperty.UpdatedBy = _authService.GetCurrentUserEmail() ?? "Unknown";

            await _firebaseService.UpdatePropertyAsync(Property.Id, existingProperty);

            // Determine action type based on status change
            string action = "updated";
            if (originalStatus == PropertyStatus.InUse && newStatus != PropertyStatus.InUse)
            {
                action = "returned";
            }
            else if (originalStatus != PropertyStatus.InUse && newStatus == PropertyStatus.InUse)
            {
                action = "borrowed";
            }

            // Notify all clients via SignalR
            await _hubContext.Clients.All.SendAsync("PropertyUpdated", existingProperty.PropertyCode, action, existingProperty.BorrowerName);

            TempData["SuccessMessage"] = $"Property {existingProperty.PropertyCode} has been updated successfully.";
            
            // Reload property data to show updated information - use GET redirect to ensure clean state
            return RedirectToPage("/Mobile/EditProperty", new { id = Property.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating property: {Id}. Exception: {Exception}", Property?.Id, ex.ToString());
            ErrorMessage = $"An error occurred while updating the property: {ex.Message}. Please try again.";
            
            // Reload property data for display
            if (Property != null && !string.IsNullOrEmpty(Property.Id))
            {
                Property = await _firebaseService.GetPropertyByIdAsync(Property.Id);
                if (Property == null)
                {
                    return RedirectToPage("/Mobile/Dashboard");
                }
                
                // Always get individual items and quantity breakdown
                try
                {
                    if (!string.IsNullOrWhiteSpace(Property.ImageUrl))
                    {
                        IndividualItems = await _firebaseService.GetIndividualItemsByImageUrlAsync(Property.ImageUrl);
                        QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByImageUrlAsync(Property.ImageUrl);
                    }
                    else if (!string.IsNullOrWhiteSpace(Property.PropertyCode))
                    {
                        IndividualItems = await _firebaseService.GetIndividualItemsByPropertyCodeAsync(Property.PropertyCode);
                        QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByPropertyCodeAsync(Property.PropertyCode);
                    }
                    else
                    {
                        // Fallback: initialize empty lists if no grouping criteria
                        IndividualItems = new List<Property> { Property };
                        QuantityBreakdown = new Dictionary<string, int>
                        {
                            { "Total", 1 },
                            { "Available", Property.Status == PropertyStatus.Available ? 1 : 0 },
                            { "InUse", Property.Status == PropertyStatus.InUse ? 1 : 0 },
                            { "UnderMaintenance", Property.Status == PropertyStatus.UnderMaintenance ? 1 : 0 },
                            { "Damaged", Property.Status == PropertyStatus.Damaged ? 1 : 0 }
                        };
                    }
                }
                catch (Exception reloadEx)
                {
                    _logger.LogError(reloadEx, "Error loading individual items in catch block: {Id}", Property.Id);
                    // Fallback: initialize with current property
                    IndividualItems = new List<Property> { Property };
                    QuantityBreakdown = new Dictionary<string, int>
                    {
                        { "Total", 1 },
                        { "Available", Property.Status == PropertyStatus.Available ? 1 : 0 },
                        { "InUse", Property.Status == PropertyStatus.InUse ? 1 : 0 },
                        { "UnderMaintenance", Property.Status == PropertyStatus.UnderMaintenance ? 1 : 0 },
                        { "Damaged", Property.Status == PropertyStatus.Damaged ? 1 : 0 }
                    };
                }
                return Page();
            }
            
            return RedirectToPage("/Mobile/Dashboard");
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
            return RedirectToPage("/Mobile/Dashboard");
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
            
            // Reload property data after return
            Property = await _firebaseService.GetPropertyByIdAsync(propertyId);
            if (Property == null)
            {
                TempData["ErrorMessage"] = "Property not found after return.";
                return RedirectToPage("/Mobile/Dashboard");
            }
            
            // Always get individual items and quantity breakdown after return
            try
            {
                if (!string.IsNullOrWhiteSpace(Property.ImageUrl))
                {
                    IndividualItems = await _firebaseService.GetIndividualItemsByImageUrlAsync(Property.ImageUrl);
                    QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByImageUrlAsync(Property.ImageUrl);
                }
                else if (!string.IsNullOrWhiteSpace(Property.PropertyCode))
                {
                    IndividualItems = await _firebaseService.GetIndividualItemsByPropertyCodeAsync(Property.PropertyCode);
                    QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByPropertyCodeAsync(Property.PropertyCode);
                }
                else
                {
                    // Fallback: initialize empty lists if no grouping criteria
                    IndividualItems = new List<Property> { Property };
                    QuantityBreakdown = new Dictionary<string, int>
                    {
                        { "Total", 1 },
                        { "Available", Property.Status == PropertyStatus.Available ? 1 : 0 },
                        { "InUse", Property.Status == PropertyStatus.InUse ? 1 : 0 },
                        { "UnderMaintenance", Property.Status == PropertyStatus.UnderMaintenance ? 1 : 0 },
                        { "Damaged", Property.Status == PropertyStatus.Damaged ? 1 : 0 }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading individual items after return: {Id}", propertyId);
                // Fallback: initialize with current property
                IndividualItems = new List<Property> { Property };
                QuantityBreakdown = new Dictionary<string, int>
                {
                    { "Total", 1 },
                    { "Available", Property.Status == PropertyStatus.Available ? 1 : 0 },
                    { "InUse", Property.Status == PropertyStatus.InUse ? 1 : 0 },
                    { "UnderMaintenance", Property.Status == PropertyStatus.UnderMaintenance ? 1 : 0 },
                    { "Damaged", Property.Status == PropertyStatus.Damaged ? 1 : 0 }
                };
            }
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error returning property: {Id}", propertyId);
            TempData["ErrorMessage"] = "An error occurred while returning the property. Please try again.";
            
            // Reload property data for display
            if (!string.IsNullOrEmpty(propertyId))
            {
                Property = await _firebaseService.GetPropertyByIdAsync(propertyId);
                if (Property == null)
                {
                    return RedirectToPage("/Mobile/Dashboard");
                }
                
                // Always get individual items and quantity breakdown
                try
                {
                    if (!string.IsNullOrWhiteSpace(Property.ImageUrl))
                    {
                        IndividualItems = await _firebaseService.GetIndividualItemsByImageUrlAsync(Property.ImageUrl);
                        QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByImageUrlAsync(Property.ImageUrl);
                    }
                    else if (!string.IsNullOrWhiteSpace(Property.PropertyCode))
                    {
                        IndividualItems = await _firebaseService.GetIndividualItemsByPropertyCodeAsync(Property.PropertyCode);
                        QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByPropertyCodeAsync(Property.PropertyCode);
                    }
                    else
                    {
                        // Fallback: initialize empty lists if no grouping criteria
                        IndividualItems = new List<Property> { Property };
                        QuantityBreakdown = new Dictionary<string, int>
                        {
                            { "Total", 1 },
                            { "Available", Property.Status == PropertyStatus.Available ? 1 : 0 },
                            { "InUse", Property.Status == PropertyStatus.InUse ? 1 : 0 },
                            { "UnderMaintenance", Property.Status == PropertyStatus.UnderMaintenance ? 1 : 0 },
                            { "Damaged", Property.Status == PropertyStatus.Damaged ? 1 : 0 }
                        };
                    }
                }
                catch (Exception reloadEx)
                {
                    _logger.LogError(reloadEx, "Error loading individual items in catch block for return: {Id}", propertyId);
                    // Fallback: initialize with current property
                    IndividualItems = new List<Property> { Property };
                    QuantityBreakdown = new Dictionary<string, int>
                    {
                        { "Total", 1 },
                        { "Available", Property.Status == PropertyStatus.Available ? 1 : 0 },
                        { "InUse", Property.Status == PropertyStatus.InUse ? 1 : 0 },
                        { "UnderMaintenance", Property.Status == PropertyStatus.UnderMaintenance ? 1 : 0 },
                        { "Damaged", Property.Status == PropertyStatus.Damaged ? 1 : 0 }
                    };
                }
            }
            return Page();
        }
    }
}



