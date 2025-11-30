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

    public string? ReturnUrl { get; set; }

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

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        // Check if running on localhost (Development environment)
        var isLocalhost = HttpContext.Request.Host.Host == "localhost" || 
                         HttpContext.Request.Host.Host == "127.0.0.1" ||
                         Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        // Step 1: Admin fills up form → Send OTP (only on localhost)
        if (ModelState.IsValid)
        {
            // Check if user already exists first (for both localhost and production)
            var userExists = await _authService.UserExistsAsync(Input.Email);
            if (userExists)
            {
                _logger.LogWarning("Registration failed: User already exists. Email: {Email}", Input.Email);
                ModelState.AddModelError(string.Empty, "Account already exists. Please login instead.");
                return Page();
            }

            if (!isLocalhost)
            {
                // On Render/Production: Direct registration without OTP
                _logger.LogInformation("=== ADMIN REGISTRATION (PRODUCTION - NO OTP) ===");
                var success = await _authService.RegisterAsync(Input.Email, Input.Password, Input.FullName);
                if (success)
                {
                    TempData["RegistrationSuccess"] = "Account created successfully! Please login.";
                    return RedirectToPage("./Login");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Registration failed. Please try again.");
                }
                return Page();
            }

            // Localhost: Send OTP
            _logger.LogInformation("=== ADMIN REGISTRATION (LOCALHOST - WITH OTP) ===");
            _logger.LogInformation("Email: {Email}, FullName: {FullName}", Input.Email, Input.FullName);

            // Generate and send OTP
            string? otpCode = null;
            bool emailSent = false;
            
            try
            {
                _logger.LogInformation("🔵 Calling GenerateAndSendOtpWithStatusAsync for {Email}", Input.Email);
                var result = await _authService.GenerateAndSendOtpWithStatusAsync(
                    Input.Email, 
                    Input.Password, 
                    Input.FullName
                );
                otpCode = result.OtpCode;
                emailSent = result.EmailSent;
                _logger.LogInformation("🔵 OTP generated: {HasOtp} (Code: {OtpCode}), Email sent: {EmailSent}", 
                    !string.IsNullOrEmpty(otpCode), otpCode, emailSent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generating/sending OTP for Email: {Email}, Error: {Error}", Input.Email, ex.Message);
                ModelState.AddModelError(string.Empty, $"Failed to send OTP: {ex.Message}. Please try again.");
                return Page();
            }

            if (!string.IsNullOrEmpty(otpCode))
            {
                _logger.LogInformation("🔵 Storing OTP data in TempData and redirecting to OTP page");
                
                // Store registration data in TempData
                TempData["EmailForVerification"] = Input.Email;
                TempData["ShowOtpInput"] = true;
                TempData["EmailSent"] = emailSent;
                TempData["RegistrationData"] = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Email = Input.Email,
                    Password = Input.Password,
                    FullName = Input.FullName
                });

                if (emailSent)
                {
                    _logger.LogInformation("✅ OTP sent successfully to {Email}", Input.Email);
                    TempData["SuccessMessage"] = "OTP sent successfully";
                }
                else
                {
                    _logger.LogWarning("⚠️ OTP generated but email not sent to {Email}. OTP: {Otp}", Input.Email, otpCode);
                    TempData["WarningMessage"] = $"OTP generated but email may not have been sent. Please check your email settings. OTP: {otpCode}";
                }

                // Redirect to VerifyOtp page
                _logger.LogInformation("🔵 Redirecting to VerifyOtp page...");
                return RedirectToPage("./VerifyOtp");
            }
            else
            {
                _logger.LogWarning("❌ Failed to generate OTP for Email: {Email}", Input.Email);
                ModelState.AddModelError(string.Empty, "Failed to generate OTP. Please try again.");
            }
        }

        return Page();
    }

}


