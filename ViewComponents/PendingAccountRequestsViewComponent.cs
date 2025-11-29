using Microsoft.AspNetCore.Mvc;
using PropertyInventory.Services;

namespace PropertyInventory.ViewComponents;

public class PendingAccountRequestsViewComponent : ViewComponent
{
    private readonly FirebaseService _firebaseService;

    public PendingAccountRequestsViewComponent(FirebaseService firebaseService)
    {
        _firebaseService = firebaseService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        try
        {
            // Only show count for admin users
            if (ViewContext.HttpContext.User?.Identity?.IsAuthenticated != true || 
                !ViewContext.HttpContext.User.IsInRole("Admin"))
            {
                return View(0);
            }

            var counts = await _firebaseService.GetAccountRequestCountsAsync();
            return View(counts.Pending);
        }
        catch (Exception)
        {
            // Return 0 if there's an error - don't crash the page
            return View(0);
        }
    }
}

