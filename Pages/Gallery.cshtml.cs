using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyInventory.Hubs;
using PropertyInventory.Models;
using PropertyInventory.Services;
using Microsoft.AspNetCore.SignalR;

namespace PropertyInventory.Pages;

[Authorize(Roles = "Admin")]
public class GalleryModel : PageModel
{
    private readonly FirebaseService _firebaseService;
    private readonly IHubContext<PropertyHub> _hubContext;

    public GalleryModel(FirebaseService firebaseService, IHubContext<PropertyHub> hubContext)
    {
        _firebaseService = firebaseService;
        _hubContext = hubContext;
    }

    public IList<Property> Properties { get; set; } = default!;
    public List<string> Categories { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchString { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? CategoryFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public PropertyStatus? StatusFilter { get; set; }

    public async Task OnGetAsync()
    {
        Categories = await _firebaseService.GetAllCategoriesAsync();
        Properties = await _firebaseService.GetGroupedPropertiesAsync(SearchString, CategoryFilter, StatusFilter);
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        try
        {
            var property = await _firebaseService.GetPropertyByIdAsync(id);
            
            if (property == null)
            {
                TempData["ErrorMessage"] = "Property not found.";
                return RedirectToPage("./Gallery");
            }

            var propertyCode = property.PropertyCode;
            await _firebaseService.DeletePropertyAsync(id);

            // Notify all clients via SignalR
            await _hubContext.Clients.All.SendAsync("PropertyDeleted", propertyCode);

            TempData["SuccessMessage"] = $"Property {propertyCode} has been deleted successfully.";
            return RedirectToPage("./Gallery");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"An error occurred while deleting the property: {ex.Message}";
            return RedirectToPage("./Gallery");
        }
    }
}

