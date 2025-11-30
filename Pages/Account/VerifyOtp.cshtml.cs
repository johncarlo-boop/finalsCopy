using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyInventory.Services;

namespace PropertyInventory.Pages.Account;

public class VerifyOtpModel : PageModel
{
    private readonly AuthService _authService;
    private readonly ILogger<VerifyOtpModel> _logger;

    public VerifyOtpModel(AuthService authService, ILogger<VerifyOtpModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [BindProperty]
    [Required(ErrorMessage = "OTP code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP code must be exactly 6 digits")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP code must be 6 digits")]
    public string OtpCode { get; set; } = string.Empty;

    [TempData]
    public string? EmailForVerification { get; set; }

    [TempData]
    public bool? EmailSent { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? WarningMessage { get; set; }

    public IActionResult OnGet()
    {
        // Load email from TempData
        EmailForVerification = TempData["EmailForVerification"] as string;
        EmailSent = TempData["EmailSent"] as bool? ?? false;
        SuccessMessage = TempData["SuccessMessage"] as string;
        WarningMessage = TempData["WarningMessage"] as string;

        // If no email in TempData, redirect to Register
        if (string.IsNullOrEmpty(EmailForVerification))
        {
            _logger.LogWarning("VerifyOtp page accessed without email in TempData. Redirecting to Register.");
            TempData["ErrorMessage"] = "Please complete registration first.";
            return RedirectToPage("./Register");
        }

        // Keep TempData for next request
        TempData.Keep("EmailForVerification");
        TempData.Keep("EmailSent");
        TempData.Keep("RegistrationData");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var emailForVerification = TempData["EmailForVerification"] as string;
        if (string.IsNullOrEmpty(emailForVerification))
        {
            ModelState.AddModelError(string.Empty, "Session expired. Please start registration again.");
            TempData["ErrorMessage"] = "Session expired. Please start registration again.";
            return RedirectToPage("./Register");
        }

        if (!ModelState.IsValid)
        {
            EmailForVerification = emailForVerification;
            TempData.Keep("EmailForVerification");
            TempData.Keep("EmailSent");
            TempData.Keep("RegistrationData");
            return Page();
        }

        _logger.LogInformation("=== ADMIN REGISTRATION - VERIFYING OTP ===");
        _logger.LogInformation("Email: {Email}, OTP: '{Otp}' (Length: {Length}, Type: {Type})", 
            emailForVerification, OtpCode, OtpCode?.Length ?? 0, OtpCode?.GetType().Name ?? "null");
        
        // Normalize OTP input - extract only digits
        var normalizedOtp = new string((OtpCode ?? string.Empty).Where(char.IsDigit).ToArray());
        if (normalizedOtp.Length != 6)
        {
            if (normalizedOtp.Length < 6)
            {
                normalizedOtp = normalizedOtp.PadLeft(6, '0');
            }
            else
            {
                normalizedOtp = normalizedOtp.Substring(0, 6);
            }
            _logger.LogInformation("OTP normalized: '{Original}' -> '{Normalized}'", OtpCode, normalizedOtp);
        }

        // Verify OTP and register
        var (success, userAlreadyExists) = await _authService.VerifyOtpAndRegisterAsync(
            emailForVerification, 
            normalizedOtp
        );

        if (success)
        {
            _logger.LogInformation("✓✓✓ OTP VERIFIED SUCCESSFULLY ✓✓✓");
            _logger.LogInformation("✓ Admin account created successfully. Email: {Email}", emailForVerification);
            
            // Clear OTP-related TempData
            TempData.Remove("EmailForVerification");
            TempData.Remove("EmailSent");
            TempData.Remove("RegistrationData");
            TempData.Remove("ShowOtpInput");
            
            TempData["RegistrationSuccess"] = "Account created successfully! Please login.";
            
            _logger.LogInformation("🔵 Redirecting to Login page...");
            return RedirectToPage("./Login");
        }
        else if (userAlreadyExists)
        {
            _logger.LogWarning("Registration failed: User already exists. Email: {Email}", emailForVerification);
            ModelState.AddModelError(string.Empty, "Account already exists. Please login instead.");
            EmailForVerification = emailForVerification;
            TempData.Keep("EmailForVerification");
            return Page();
        }
        else
        {
            _logger.LogWarning("OTP verification failed for Email: {Email}, OTP: {Otp}", emailForVerification, OtpCode);
            ModelState.AddModelError(string.Empty, "Invalid or expired OTP code. Please try again.");
            EmailForVerification = emailForVerification;
            TempData.Keep("EmailForVerification");
            TempData.Keep("EmailSent");
            TempData.Keep("RegistrationData");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostResendOtpAsync()
    {
        var emailForVerification = TempData["EmailForVerification"] as string;
        if (string.IsNullOrEmpty(emailForVerification))
        {
            TempData["ErrorMessage"] = "Session expired. Please start registration again.";
            return RedirectToPage("./Register");
        }

        // Get registration data from TempData
        var registrationDataJson = TempData["RegistrationData"] as string;
        if (string.IsNullOrEmpty(registrationDataJson))
        {
            TempData["ErrorMessage"] = "Session expired. Please start registration again.";
            return RedirectToPage("./Register");
        }

        // Deserialize registration data
        var registrationData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(registrationDataJson);
        var email = registrationData.GetProperty("Email").GetString() ?? emailForVerification;
        var password = registrationData.GetProperty("Password").GetString() ?? string.Empty;
        var fullName = registrationData.GetProperty("FullName").GetString() ?? string.Empty;

        _logger.LogInformation("=== RESENDING OTP ===");
        _logger.LogInformation("Email: {Email}", email);

        // Generate and send new OTP
        var (otpCode, emailSent) = await _authService.GenerateAndSendOtpWithStatusAsync(
            email,
            password,
            fullName
        );

        if (otpCode != null)
        {
            // Store registration data back in TempData
            TempData["EmailForVerification"] = email;
            TempData["EmailSent"] = emailSent;
            TempData["RegistrationData"] = registrationDataJson;

            if (emailSent)
            {
                _logger.LogInformation("OTP resent successfully to {Email}", email);
                TempData["SuccessMessage"] = "OTP sent successfully";
            }
            else
            {
                _logger.LogWarning("OTP generated but email not sent to {Email}. OTP: {Otp}", email, otpCode);
                TempData["WarningMessage"] = $"OTP generated but email may not have been sent. Please check your email settings. OTP: {otpCode}";
            }
        }
        else
        {
            _logger.LogWarning("Failed to resend OTP for Email: {Email}", email);
            TempData["ErrorMessage"] = "Failed to resend OTP. Please try again.";
        }

        return RedirectToPage();
    }
}

