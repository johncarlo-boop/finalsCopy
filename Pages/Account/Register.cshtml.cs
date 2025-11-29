using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyInventory.Services;

namespace PropertyInventory.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly AuthService _authService;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(AuthService authService, ILogger<RegisterModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    [BindProperty]
    public string? OtpCode { get; set; }

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? EmailForVerification { get; set; }

    [TempData]
    public bool? ShowOtpInput { get; set; }

    [TempData]
    public string? OtpCodeDisplay { get; set; }

    [TempData]
    public bool? EmailSent { get; set; }

    public class InputModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet(string? returnUrl = null, bool reset = false)
    {
        ReturnUrl = returnUrl;
        
        // Clear any OTP-related TempData (no longer needed)
            TempData.Remove("ShowOtpInput");
            TempData.Remove("EmailForVerification");
            TempData.Remove("RegistrationData");
            TempData.Remove("OtpCode");
            TempData.Remove("OtpCodeDisplay");
            TempData.Remove("EmailSent");
            TempData.Remove("OtpResent");
        
        ShowOtpInput = false;
        EmailForVerification = null;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        // Direct registration without OTP verification
        if (ModelState.IsValid)
        {
            _logger.LogInformation("=== DIRECT REGISTRATION START (NO OTP) ===");
            _logger.LogInformation("Email: {Email}, FullName: {FullName}", Input.Email, Input.FullName);
            
            // Check if user already exists first
            var userExists = await _authService.UserExistsAsync(Input.Email);
            if (userExists)
            {
                _logger.LogWarning("Registration failed: User already exists. Email: {Email}", Input.Email);
                ModelState.AddModelError(string.Empty, "Account already exists. Please login instead.");
                return Page();
            }

            // Direct registration - no OTP verification needed
            var success = await _authService.RegisterAsync(Input.Email, Input.Password, Input.FullName);

            if (success)
            {
                _logger.LogInformation("✓ User created successfully. Email: {Email}", Input.Email);
                TempData["RegistrationSuccess"] = "Account created successfully! Please login.";
                return RedirectToPage("./Login");
            }
            else
            {
                _logger.LogWarning("Registration failed for Email: {Email}", Input.Email);
                ModelState.AddModelError(string.Empty, "Registration failed. Please try again.");
            }
        }

        return Page();
    }

}


