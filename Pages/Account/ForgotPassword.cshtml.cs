using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyInventory.Services;

namespace PropertyInventory.Pages.Account;

public class ForgotPasswordModel : PageModel
{
    private readonly AuthService _authService;
    private readonly ILogger<ForgotPasswordModel> _logger;

    public ForgotPasswordModel(AuthService authService, ILogger<ForgotPasswordModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ModelState.IsValid)
        {
            var result = await _authService.ForgotPasswordAsync(Input.Email);

            if (result.Success)
            {
                _logger.LogInformation("Temporary password sent to {Email}", Input.Email);
                TempData["Success"] = "A temporary password has been sent to your email. Please check your inbox and login with the temporary password. You will be required to change it after logging in.";
                return RedirectToPage("./Login");
            }
            else
            {
                // Don't reveal if email exists or not for security
                TempData["Error"] = "If an account with that email exists, a temporary password has been sent.";
                return Page();
            }
        }

        return Page();
    }
}


