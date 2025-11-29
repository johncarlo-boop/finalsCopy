using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyInventory.Hubs;
using PropertyInventory.Models;
using PropertyInventory.Services;
using Microsoft.AspNetCore.SignalR;

namespace PropertyInventory.Pages;

[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly FirebaseService _firebaseService;
    private readonly IHubContext<PropertyHub> _hubContext;

    public EditModel(FirebaseService firebaseService, IHubContext<PropertyHub> hubContext)
    {
        _firebaseService = firebaseService;
        _hubContext = hubContext;
    }

    [BindProperty]
    public Property Property { get; set; } = default!;

    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> StatusOptions { get; set; } = new();
    public Dictionary<string, int>? QuantityBreakdown { get; set; }
    public List<Property>? IndividualItems { get; set; }

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var property = await _firebaseService.GetPropertyByIdAsync(id);
        if (property == null)
        {
            return NotFound();
        }
        Property = property;
        
        // Get quantity breakdown and individual items
        if (!string.IsNullOrWhiteSpace(Property.ImageUrl))
        {
            IndividualItems = await _firebaseService.GetIndividualItemsByImageUrlAsync(Property.ImageUrl);
            QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByImageUrlAsync(Property.ImageUrl);
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
        
        // Set Property.Quantity to total quantity for editing (so user can see and edit total)
        Property.Quantity = QuantityBreakdown.ContainsKey("Total") ? QuantityBreakdown["Total"] : Property.Quantity;
        
        // Create filtered status options (exclude Lost and Disposed)
        StatusOptions = Enum.GetValues(typeof(PropertyStatus))
            .Cast<PropertyStatus>()
            .Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Text = s.ToString(),
                Value = s.ToString(),
                Selected = Property.Status == s
            })
            .ToList();
        
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Create filtered status options (exclude Lost and Disposed)
        StatusOptions = Enum.GetValues(typeof(PropertyStatus))
            .Cast<PropertyStatus>()
            .Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Text = s.ToString(),
                Value = s.ToString(),
                Selected = Property.Status == s
            })
            .ToList();

        if (!ModelState.IsValid)
        {
            // Re-populate quantity breakdown and individual items on validation error
            if (!string.IsNullOrWhiteSpace(Property.ImageUrl))
            {
                IndividualItems = await _firebaseService.GetIndividualItemsByImageUrlAsync(Property.ImageUrl);
                QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByImageUrlAsync(Property.ImageUrl);
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

        if (string.IsNullOrEmpty(Property.Id))
        {
            return NotFound();
        }

        // Check if property code already exists (excluding current property)
        if (await _firebaseService.PropertyCodeExistsAsync(Property.PropertyCode, Property.Id))
        {
            ModelState.AddModelError("Property.PropertyCode", "Property code already exists.");
            return Page();
        }

        // Get the original property to check if status changed
        var originalProperty = await _firebaseService.GetPropertyByIdAsync(Property.Id);
        if (originalProperty == null)
        {
            TempData["ErrorMessage"] = "Property not found.";
            return RedirectToPage("./Index");
        }

        // Check if property is currently borrowed - prevent editing if borrowed (unless returning it)
        // Allow editing only if status is being changed from InUse to something else (returning)
        if (originalProperty.Status == PropertyStatus.InUse && 
            !string.IsNullOrWhiteSpace(originalProperty.BorrowerName) &&
            Property.Status == PropertyStatus.InUse)
        {
            // Property is borrowed and status remains InUse - prevent editing
            TempData["ErrorMessage"] = $"This property is currently borrowed by {originalProperty.BorrowerName}. Please return it first (change status from 'InUse') before editing other fields.";
            
            // Re-populate data for display
            Property = originalProperty;
            if (!string.IsNullOrWhiteSpace(Property.ImageUrl))
            {
                IndividualItems = await _firebaseService.GetIndividualItemsByImageUrlAsync(Property.ImageUrl);
                QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByImageUrlAsync(Property.ImageUrl);
            }
            else
            {
                IndividualItems = await _firebaseService.GetIndividualItemsByPropertyCodeAsync(Property.PropertyCode);
                QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByPropertyCodeAsync(Property.PropertyCode);
            }
            
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
            
            StatusOptions = Enum.GetValues(typeof(PropertyStatus))
                .Cast<PropertyStatus>()
                .Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Text = s.ToString(),
                    Value = s.ToString(),
                    Selected = Property.Status == s
                })
                .ToList();
            
            return Page();
        }

        // Get current total quantity from database
        int currentTotalQuantity = 1;
        if (!string.IsNullOrWhiteSpace(Property.ImageUrl))
        {
            var existingItems = await _firebaseService.GetIndividualItemsByImageUrlAsync(Property.ImageUrl);
            currentTotalQuantity = existingItems?.Count ?? 1;
        }
        else
        {
            var existingItems = await _firebaseService.GetIndividualItemsByPropertyCodeAsync(Property.PropertyCode);
            currentTotalQuantity = existingItems?.Count ?? 1;
        }
        
        // Handle quantity changes - if quantity increased, create new property entries
        if (Property.Quantity > currentTotalQuantity)
        {
            var quantityToAdd = Property.Quantity - currentTotalQuantity;
            
            // Get existing properties with same ImageUrl or PropertyCode to determine next numbers
            var existingProperties = new List<Property>();
            int nextTagNumber = 1;
            int nextPropertyCodeNumber = 1;
            
            if (!string.IsNullOrWhiteSpace(Property.ImageUrl))
            {
                existingProperties = await _firebaseService.GetPropertiesByImageUrlAsync(Property.ImageUrl.Trim());
            }
            else
            {
                existingProperties = await _firebaseService.GetIndividualItemsByPropertyCodeAsync(Property.PropertyCode);
            }
            
            // Find the highest tag number and property code number
            if (existingProperties.Any())
            {
                var tagNumbers = new List<int>();
                var propertyCodeNumbers = new List<int>();
                
                foreach (var prop in existingProperties)
                {
                    // Extract tag number
                    if (!string.IsNullOrWhiteSpace(prop.SerialNumber))
                    {
                        var serialParts = prop.SerialNumber.Split('-');
                        if (serialParts.Length > 0)
                        {
                            var lastPart = serialParts[serialParts.Length - 1];
                            if (int.TryParse(lastPart, out int tagNum))
                            {
                                tagNumbers.Add(tagNum);
                            }
                        }
                    }
                    
                    // Extract property code number
                    if (!string.IsNullOrWhiteSpace(prop.PropertyCode))
                    {
                        var codeParts = prop.PropertyCode.Split('-');
                        if (codeParts.Length >= 2 && int.TryParse(codeParts[codeParts.Length - 1], out int codeNum))
                        {
                            propertyCodeNumbers.Add(codeNum);
                        }
                    }
                }
                
                if (tagNumbers.Any())
                {
                    nextTagNumber = tagNumbers.Max() + 1;
                }
                if (propertyCodeNumbers.Any())
                {
                    nextPropertyCodeNumber = propertyCodeNumbers.Max() + 1;
                }
            }
            
            // Determine base property code and tag number
            var basePropertyCode = Property.PropertyCode;
            var baseCodeParts = basePropertyCode.Split('-');
            if (baseCodeParts.Length >= 2)
            {
                basePropertyCode = $"{baseCodeParts[0]}-{baseCodeParts[1]}";
            }
            
            var baseTagNumber = !string.IsNullOrWhiteSpace(Property.SerialNumber) 
                ? Property.SerialNumber.Trim() 
                : $"{basePropertyCode}-TAG";
            
            // Remove tag number suffix if it exists
            var baseTagParts = baseTagNumber.Split('-');
            if (baseTagParts.Length > 1 && int.TryParse(baseTagParts[baseTagParts.Length - 1], out _))
            {
                baseTagNumber = string.Join("-", baseTagParts.Take(baseTagParts.Length - 1));
            }
            
            // Create new property entries for the additional quantity
            for (int i = 0; i < quantityToAdd; i++)
            {
                var tagNumber = $"{baseTagNumber}-{(nextTagNumber + i):D3}";
                var propertyCode = $"{basePropertyCode}-{(nextPropertyCodeNumber + i):D3}";
                
                var newProperty = new Property
                {
                    PropertyCode = propertyCode,
                    PropertyName = Property.PropertyName?.Trim() ?? string.Empty,
                    Category = Property.Category?.Trim() ?? string.Empty,
                    Description = Property.Description?.Trim(),
                    Location = Property.Location?.Trim() ?? string.Empty,
                    Status = PropertyStatus.Available, // New items default to Available
                    Quantity = 1,
                    DateReceived = Property.DateReceived,
                    SerialNumber = tagNumber,
                    ImageUrl = Property.ImageUrl?.Trim(),
                    LastUpdated = DateTime.UtcNow,
                    UpdatedBy = User.Identity?.Name ?? "Unknown",
                    Remarks = Property.Remarks?.Trim()
                };
                
                // Ensure DateReceived is in UTC
                if (newProperty.DateReceived.HasValue && newProperty.DateReceived.Value.Kind != DateTimeKind.Utc)
                {
                    newProperty.DateReceived = newProperty.DateReceived.Value.ToUniversalTime();
                }
                
                await _firebaseService.CreatePropertyAsync(newProperty);
                await _hubContext.Clients.All.SendAsync("PropertyCreated", newProperty.PropertyCode);
            }
            
            // Update the original property with the new quantity (but keep it as 1 since each item is separate)
            Property.Quantity = 1; // Each item is quantity 1
        }
        else if (Property.Quantity < originalProperty.Quantity)
        {
            // If quantity decreased, we should not delete items automatically
            // Just update the current property quantity to 1 (since each item is separate)
            Property.Quantity = 1;
        }
        else
        {
            // Quantity unchanged, keep it as 1
            Property.Quantity = 1;
        }

        // Handle status changes for borrow/return
        if (originalProperty.Status == PropertyStatus.InUse && Property.Status != PropertyStatus.InUse)
        {
            // Status changed from InUse to something else - clear borrower info (return)
            Property.BorrowerName = null;
            Property.BorrowedDate = null;
            Property.ReturnDate = null;
            Property.OverdueNotificationSent = false;
        }
        else if (originalProperty.Status != PropertyStatus.InUse && Property.Status == PropertyStatus.InUse)
        {
            // Status changed to InUse - automatically mark as borrowed
            // Set borrow date if not set
            if (!Property.BorrowedDate.HasValue)
            {
                Property.BorrowedDate = DateTime.UtcNow;
            }
            // Ensure borrower name is required when status is InUse
            if (string.IsNullOrWhiteSpace(Property.BorrowerName))
            {
                ModelState.AddModelError("Property.BorrowerName", "Borrower name is required when status is InUse.");
                // Re-populate data for validation error
                if (!string.IsNullOrWhiteSpace(Property.ImageUrl))
                {
                    IndividualItems = await _firebaseService.GetIndividualItemsByImageUrlAsync(Property.ImageUrl);
                    QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByImageUrlAsync(Property.ImageUrl);
                }
                else
                {
                    IndividualItems = await _firebaseService.GetIndividualItemsByPropertyCodeAsync(Property.PropertyCode);
                    QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByPropertyCodeAsync(Property.PropertyCode);
                }
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
                StatusOptions = Enum.GetValues(typeof(PropertyStatus))
                    .Cast<PropertyStatus>()
                    .Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Text = s.ToString(),
                        Value = s.ToString(),
                        Selected = Property.Status == s
                    })
                    .ToList();
                return Page();
            }
            Property.OverdueNotificationSent = false;
        }
        // If borrower name is provided but status is not InUse, automatically set status to InUse
        else if (!string.IsNullOrWhiteSpace(Property.BorrowerName) && Property.Status != PropertyStatus.InUse && originalProperty.Status != PropertyStatus.InUse)
        {
            // Borrower name provided but status not InUse - automatically set to InUse (borrowing)
            Property.Status = PropertyStatus.InUse;
            if (!Property.BorrowedDate.HasValue)
            {
                Property.BorrowedDate = DateTime.UtcNow;
            }
            Property.OverdueNotificationSent = false;
        }
        // If status is already InUse and borrower info is being updated, keep it as is
        else if (Property.Status == PropertyStatus.InUse && originalProperty.Status == PropertyStatus.InUse)
        {
            // Status remains InUse - just update borrower info if provided
            // Ensure borrowed date is set if borrower name is provided but date is not
            if (!string.IsNullOrWhiteSpace(Property.BorrowerName) && !Property.BorrowedDate.HasValue)
            {
                Property.BorrowedDate = DateTime.UtcNow;
            }
            // Allow updating return date even if already borrowed
            // The return date can be updated at any time
        }
        
        // Ensure BorrowedDate and ReturnDate are in UTC if they have values
        if (Property.BorrowedDate.HasValue && Property.BorrowedDate.Value.Kind != DateTimeKind.Utc)
        {
            Property.BorrowedDate = Property.BorrowedDate.Value.ToUniversalTime();
        }
        if (Property.ReturnDate.HasValue && Property.ReturnDate.Value.Kind != DateTimeKind.Utc)
        {
            Property.ReturnDate = Property.ReturnDate.Value.ToUniversalTime();
        }

        // Ensure LastUpdated is in UTC
        Property.LastUpdated = DateTime.UtcNow;
        Property.UpdatedBy = User.Identity?.Name ?? "Unknown";
        
        // Ensure DateReceived is in UTC if it has a value
        if (Property.DateReceived.HasValue && Property.DateReceived.Value.Kind != DateTimeKind.Utc)
        {
            Property.DateReceived = Property.DateReceived.Value.ToUniversalTime();
        }

        try
        {
            await _firebaseService.UpdatePropertyAsync(Property.Id, Property);
            
            // Determine action type based on status change
            string action = "updated";
            string successMessage = $"Property {Property.PropertyCode} (Tag: {Property.SerialNumber}) has been updated successfully.";
            
            if (originalProperty.Status == PropertyStatus.InUse && Property.Status != PropertyStatus.InUse)
            {
                action = "returned";
                successMessage = $"Property {Property.PropertyCode} (Tag: {Property.SerialNumber}) has been marked as returned.";
            }
            else if (originalProperty.Status != PropertyStatus.InUse && Property.Status == PropertyStatus.InUse)
            {
                action = "borrowed";
                successMessage = $"Property {Property.PropertyCode} (Tag: {Property.SerialNumber}) has been borrowed by {Property.BorrowerName}.";
                if (Property.ReturnDate.HasValue)
                {
                    var returnDateLocal = Property.ReturnDate.Value.ToLocalTime();
                    successMessage += $" Return date: {returnDateLocal:MM/dd/yyyy hh:mm tt}.";
                }
            }
            else if (Property.Status == PropertyStatus.InUse && originalProperty.Status == PropertyStatus.InUse)
            {
                // Borrowing information was updated
                if (!string.IsNullOrWhiteSpace(Property.BorrowerName))
                {
                    action = "borrowing_updated";
                    successMessage = $"Borrowing information for {Property.PropertyCode} (Tag: {Property.SerialNumber}) has been updated.";
                    if (Property.BorrowerName != originalProperty.BorrowerName)
                    {
                        successMessage += $" Borrower: {Property.BorrowerName}.";
                    }
                    if (Property.ReturnDate.HasValue && Property.ReturnDate != originalProperty.ReturnDate)
                    {
                        var returnDateLocal = Property.ReturnDate.Value.ToLocalTime();
                        successMessage += $" Return date updated to: {returnDateLocal:MM/dd/yyyy hh:mm tt}.";
                    }
                }
            }
            
            // Notify all clients via SignalR
            await _hubContext.Clients.All.SendAsync("PropertyUpdated", Property.PropertyCode, action, Property.BorrowerName);
            
            TempData["SuccessMessage"] = successMessage;
            
            // Check if this is part of a grouped property (has ImageUrl with multiple items)
            // If so, redirect back to Details page to show all items
            if (!string.IsNullOrWhiteSpace(Property.ImageUrl))
            {
                var itemsWithSameImage = await _firebaseService.GetIndividualItemsByImageUrlAsync(Property.ImageUrl);
                if (itemsWithSameImage != null && itemsWithSameImage.Count > 1)
                {
                    return RedirectToPage("./Details", new { id = Property.Id });
                }
            }
        }
        catch (Exception ex)
        {
            var property = await _firebaseService.GetPropertyByIdAsync(Property.Id);
            if (property == null)
            {
                TempData["ErrorMessage"] = "Property not found.";
                return RedirectToPage("./Index");
            }
            TempData["ErrorMessage"] = $"An error occurred while updating the property: {ex.Message}";
            Property = property;
            
            // Re-populate quantity breakdown and individual items
            if (!string.IsNullOrWhiteSpace(Property.ImageUrl))
            {
                IndividualItems = await _firebaseService.GetIndividualItemsByImageUrlAsync(Property.ImageUrl);
                QuantityBreakdown = await _firebaseService.GetQuantityBreakdownByImageUrlAsync(Property.ImageUrl);
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
            
            // Re-populate status options
            StatusOptions = Enum.GetValues(typeof(PropertyStatus))
                .Cast<PropertyStatus>()
                .Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Text = s.ToString(),
                    Value = s.ToString(),
                    Selected = Property.Status == s
                })
                .ToList();
            
            return Page();
        }

        return RedirectToPage("./Index");
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
            return RedirectToPage("./Edit", new { id });
        }
    }
}

