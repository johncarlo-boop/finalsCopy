using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyInventory.Hubs;
using PropertyInventory.Models;
using PropertyInventory.Services;
using Microsoft.AspNetCore.SignalR;

namespace PropertyInventory.Pages;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly FirebaseService _firebaseService;
    private readonly IHubContext<PropertyHub> _hubContext;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(FirebaseService firebaseService, IHubContext<PropertyHub> hubContext, ILogger<CreateModel> logger)
    {
        _firebaseService = firebaseService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public IActionResult OnGet()
    {
        return Page();
    }

    [BindProperty]
    public Property Property { get; set; } = default!;

    [BindProperty]
    public int Quantity { get; set; } = 1;

    private async Task<string> GeneratePropertyCodeAsync()
    {
        try
    {
        // Get all properties
        var allProperties = await _firebaseService.GetAllPropertiesAsync();
        var existingCodes = allProperties
                .Where(p => !string.IsNullOrEmpty(p.PropertyCode) && p.PropertyCode.StartsWith("PROP-"))
            .Select(p => p.PropertyCode)
            .ToList();

        int nextNumber = 1;
        
        if (existingCodes.Any())
        {
            // Extract all numbers from property codes
            var numbers = new List<int>();
            foreach (var code in existingCodes)
            {
                    if (string.IsNullOrEmpty(code)) continue;
                    
                // Handle formats like "PROP-001" or "PROP-001-001"
                var parts = code.Split('-');
                if (parts.Length >= 2 && int.TryParse(parts[1], out int number))
                {
                    numbers.Add(number);
                }
            }
            
            if (numbers.Any())
            {
                nextNumber = numbers.Max() + 1;
            }
        }

        return $"PROP-{nextNumber:D3}";
        }
        catch (Exception)
        {
            // If there's an error, start from PROP-001
            return "PROP-001";
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
        if (!ModelState.IsValid || Property == null)
        {
            return Page();
        }

        if (Quantity < 1)
        {
            ModelState.AddModelError("Quantity", "Quantity must be at least 1.");
            return Page();
        }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(Property.PropertyName))
            {
                ModelState.AddModelError("Property.PropertyName", "Property Name is required.");
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Property.Category))
            {
                ModelState.AddModelError("Property.Category", "Category is required.");
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Property.Location))
            {
                ModelState.AddModelError("Property.Location", "Location is required.");
                return Page();
            }

        var lastUpdated = DateTime.UtcNow;
            var updatedBy = User.Identity?.Name ?? "Unknown";

            // Convert DateReceived to UTC if it has a value
            DateTime? dateReceivedUtc = null;
            if (Property.DateReceived.HasValue)
            {
                var dateReceived = Property.DateReceived.Value;
                if (dateReceived.Kind == DateTimeKind.Unspecified)
                {
                    // If kind is Unspecified, assume it's local time and convert to UTC
                    dateReceivedUtc = dateReceived.ToUniversalTime();
                }
                else if (dateReceived.Kind == DateTimeKind.Local)
                {
                    dateReceivedUtc = dateReceived.ToUniversalTime();
                }
                else
                {
                    dateReceivedUtc = dateReceived; // Already UTC
                }
            }

            // Get existing properties with same ImageUrl to determine next tag number
            var existingProperties = new List<Property>();
            int nextTagNumber = 1;
            
            if (!string.IsNullOrWhiteSpace(Property.ImageUrl))
            {
                existingProperties = await _firebaseService.GetPropertiesByImageUrlAsync(Property.ImageUrl.Trim());
                
                // Find the highest tag number from existing properties
                if (existingProperties.Any())
                {
                    var tagNumbers = new List<int>();
                    foreach (var prop in existingProperties)
                    {
                        if (!string.IsNullOrWhiteSpace(prop.SerialNumber))
                        {
                            // Extract the last number from tag (e.g., "TAG-001" -> 1, "PROP-001-TAG-010" -> 10)
                            var parts = prop.SerialNumber.Split('-');
                            if (parts.Length > 0)
                            {
                                var lastPart = parts[parts.Length - 1];
                                if (int.TryParse(lastPart, out int num))
                                {
                                    tagNumbers.Add(num);
                                }
                            }
                        }
                    }
                    
                    if (tagNumbers.Any())
                    {
                        nextTagNumber = tagNumbers.Max() + 1;
                    }
                }
            }

            // Always create separate property entries, each with unique tag number
            var propertiesToAdd = new List<Property>();
            var basePropertyCode = await GeneratePropertyCodeAsync();
            
            // If same ImageUrl exists, use the same base property code
            if (existingProperties.Any())
            {
                var firstExisting = existingProperties.First();
                var baseCodeParts = firstExisting.PropertyCode.Split('-');
                if (baseCodeParts.Length >= 2)
                {
                    basePropertyCode = $"{baseCodeParts[0]}-{baseCodeParts[1]}";
                }
            }

            // Generate base tag number prefix
            var baseTagNumber = !string.IsNullOrWhiteSpace(Property.SerialNumber) 
                ? Property.SerialNumber.Trim() 
                : $"{basePropertyCode}-TAG";

        // Create multiple property items based on quantity, each with unique tag number
        for (int i = 0; i < Quantity; i++)
        {
            var tagNumber = $"{baseTagNumber}-{(nextTagNumber + i):D3}";
            
            var property = new Property
            {
                PropertyCode = Quantity > 1 ? $"{basePropertyCode}-{(nextTagNumber + i):D3}" : basePropertyCode,
                    PropertyName = Property.PropertyName?.Trim() ?? string.Empty,
                    Category = Property.Category?.Trim() ?? string.Empty,
                    Description = Property.Description?.Trim(),
                    Location = Property.Location?.Trim() ?? string.Empty,
                Status = Property.Status,
                Quantity = 1, // Each item is quantity 1
                    DateReceived = dateReceivedUtc,
                    SerialNumber = tagNumber, // Each item gets unique tag number
                    ImageUrl = Property.ImageUrl?.Trim(),
                    LastUpdated = lastUpdated, // Already UTC
                UpdatedBy = updatedBy,
                    Remarks = Property.Remarks?.Trim()
            };

            // Check if property code already exists (shouldn't happen, but just in case)
            if (await _firebaseService.PropertyCodeExistsAsync(property.PropertyCode))
            {
                // If code exists, generate a new one
                basePropertyCode = await GeneratePropertyCodeAsync();
                property.PropertyCode = Quantity > 1 ? $"{basePropertyCode}-{(nextTagNumber + i):D3}" : basePropertyCode;
            }

            propertiesToAdd.Add(property);
        }

        // Add all properties to Firebase
        foreach (var prop in propertiesToAdd)
            {
                try
        {
            await _firebaseService.CreatePropertyAsync(prop);
            // Notify all clients via SignalR
            await _hubContext.Clients.All.SendAsync("PropertyCreated", prop.PropertyCode);
                }
                catch (Exception ex)
                {
                    // Log error but continue with other properties
                    _logger.LogError(ex, "Error creating property {PropertyCode}: {Error}", prop.PropertyCode, ex.Message);
                    ModelState.AddModelError(string.Empty, $"Error creating property {prop.PropertyCode}: {ex.Message}");
                    return Page();
                }
        }

        // Redirect to Index page to show all created items
        if (Quantity > 1)
        {
            TempData["SuccessMessage"] = $"Successfully created {Quantity} property items, each with a unique ID!";
        }
            else
            {
                TempData["SuccessMessage"] = "Property created successfully!";
            }
        return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnPostAsync: {Error}", ex.Message);
            _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
            ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
            return Page();
        }
    }
}

