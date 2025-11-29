using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyInventory.Services;

namespace PropertyInventory.Pages.Account;

public class LogoutModel : PageModel
{
    private readonly AuthService _authService;
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(AuthService authService, ILogger<LogoutModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPost(string? returnUrl = null)
    {
        await _authService.LogoutAsync();
        _logger.LogInformation("User logged out.");
        if (returnUrl != null)
        {
            return LocalRedirect(returnUrl);
        }
        else
        {
            return RedirectToPage("/Account/Login");
        }
    }
}