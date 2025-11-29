using Microsoft.AspNetCore.Mvc;
using PropertyInventory.Services;

namespace PropertyInventory.ViewComponents;

public class ProfilePictureViewComponent : ViewComponent
{
    private readonly AuthService _authService;

    public ProfilePictureViewComponent(AuthService authService)
    {
        _authService = authService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        string? profilePicturePath = null;
        
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = _authService.GetCurrentUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _authService.GetUserByIdAsync(userId);
                profilePicturePath = user?.ProfilePicturePath;
            }
        }

        return View(model: profilePicturePath);
    }
}
