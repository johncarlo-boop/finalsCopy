using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using PropertyInventory.Models;

namespace PropertyInventory.Services;

public class AuthService
{
    private readonly FirebaseService _firebaseService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly EmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(FirebaseService firebaseService, IHttpContextAccessor httpContextAccessor, EmailService emailService, ILogger<AuthService> logger)
    {
        _firebaseService = firebaseService;
        _httpContextAccessor = httpContextAccessor;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<(bool Success, ApplicationUser? User)> LoginAsync(string email, string password, bool rememberMe = false)
    {
        // Normalize email for consistent lookup
        var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;
        var user = await _firebaseService.GetUserByEmailAsync(normalizedEmail);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found for email: {Email}", email);
            return (false, null);
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid password for email: {Email}", email);
            return (false, null);
        }

        // Check if user is approved (for account requests)
        if (!user.IsApproved)
        {
            _logger.LogWarning("Login failed: User not approved for email: {Email}", email);
            return (false, null);
        }
        
        _logger.LogInformation("Login successful for user: {Email}, UserType: {UserType}, IsAdmin: {IsAdmin}", 
            user.Email, user.UserType, user.IsAdmin);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("FullName", user.FullName),
            new Claim("UserType", user.UserType.ToString())
        };

        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        // Add claim if password change is required
        if (user.RequiresPasswordChange)
        {
            claims.Add(new Claim("RequiresPasswordChange", "true"));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
        };

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        return (true, user);
    }

    public async Task<(bool Success, string? TemporaryPassword)> ForgotPasswordAsync(string email)
    {
        var user = await _firebaseService.GetUserByEmailAsync(email);

        if (user == null)
        {
            return (false, null); // User not found
        }

        // Generate temporary password (8 characters: 4 letters + 4 numbers)
        var random = new Random();
        var letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var numbers = "0123456789";
        var tempPassword = new string(Enumerable.Repeat(letters, 4).Select(s => s[random.Next(s.Length)]).ToArray()) +
                          new string(Enumerable.Repeat(numbers, 4).Select(s => s[random.Next(s.Length)]).ToArray());
        
        // Shuffle the password
        tempPassword = new string(tempPassword.OrderBy(x => random.Next()).ToArray());

        // Hash the temporary password
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(tempPassword);

        // Update user password and set flag for password change
        user.PasswordHash = hashedPassword;
        user.RequiresPasswordChange = true;

        await _firebaseService.UpdateUserAsync(user.Id, user);

        // Send email with temporary password
        var emailSent = await _emailService.SendTemporaryPasswordEmailAsync(email, tempPassword, user.FullName);

        if (emailSent)
        {
            _logger.LogInformation("Temporary password sent to {Email}", email);
            return (true, tempPassword);
        }
        else
        {
            _logger.LogWarning("Failed to send temporary password email to {Email}", email);
            // Still return success since password was reset, but email might not have been sent
            return (true, tempPassword);
        }
    }

    public async Task<(string? OtpCode, bool EmailSent)> GenerateAndSendOtpWithStatusAsync(string email, string password, string fullName)
    {
        // Check if user already exists
        if (await _firebaseService.UserExistsAsync(email))
        {
            return (null, false);
        }

        // Generate 6-digit OTP (always exactly 6 digits)
        var random = new Random();
        var otpCode = random.Next(100000, 999999).ToString("D6"); // Always 6 digits (100000-999999)
        
        // Ensure it's exactly 6 digits and clean (no whitespace)
        otpCode = otpCode.Trim();
        if (otpCode.Length != 6)
        {
            // Fallback: ensure 6 digits
            if (int.TryParse(otpCode, out var num))
            {
                otpCode = num.ToString("D6");
            }
            else
            {
                otpCode = otpCode.PadLeft(6, '0').Substring(0, 6);
            }
        }

        // Normalize email to lowercase for consistent comparison
        var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;

        // Create OTP verification record
        var otpVerification = new OtpVerification
        {
            Email = normalizedEmail,
            OtpCode = otpCode, // Store as clean 6-digit string
            FullName = fullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10), // OTP expires in 10 minutes
            IsUsed = false
        };
        
        _logger.LogInformation("=== OTP CREATION ===");
        _logger.LogInformation("Created OTP: Email='{Email}', OTP='{OtpCode}' (Length={Length}, Type={Type}), ExpiresAt={ExpiresAt}", 
            normalizedEmail, otpCode, otpCode.Length, otpCode.GetType().Name, otpVerification.ExpiresAt);

        await _firebaseService.CreateOtpVerificationAsync(otpVerification);

        // Send OTP via email and get status
        var emailSent = await _emailService.SendOtpEmailAsync(email ?? normalizedEmail, otpCode, fullName);
        
        if (emailSent)
        {
            _logger.LogInformation("OTP email sent successfully to {Email}", email);
        }
        else
        {
            _logger.LogWarning("Failed to send OTP email to {Email}, but OTP generated: {Otp}. Please configure email settings in appsettings.json", email, otpCode);
        }

        return (otpCode, emailSent);
    }

    public async Task<string?> GenerateAndSendOtpAsync(string email, string password, string fullName)
    {
        // Check if user already exists
        if (await _firebaseService.UserExistsAsync(email))
        {
            return null;
        }

        // Generate 6-digit OTP (always exactly 6 digits)
        var random = new Random();
        var otpCode = random.Next(100000, 999999).ToString("D6"); // Always 6 digits (100000-999999)
        
        // Ensure it's exactly 6 digits and clean (no whitespace)
        otpCode = otpCode.Trim();
        if (otpCode.Length != 6)
        {
            // Fallback: ensure 6 digits
            if (int.TryParse(otpCode, out var num))
            {
                otpCode = num.ToString("D6");
            }
            else
            {
                otpCode = otpCode.PadLeft(6, '0').Substring(0, 6);
            }
        }

        // Normalize email to lowercase for consistent comparison
        var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;

        // Create OTP verification record
        var otpVerification = new OtpVerification
        {
            Email = normalizedEmail,
            OtpCode = otpCode, // Store as clean 6-digit string
            FullName = fullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10), // OTP expires in 10 minutes
            IsUsed = false
        };
        
        _logger.LogInformation("=== OTP CREATION ===");
        _logger.LogInformation("Created OTP: Email='{Email}', OTP='{OtpCode}' (Length={Length}, Type={Type}), ExpiresAt={ExpiresAt}", 
            normalizedEmail, otpCode, otpCode.Length, otpCode.GetType().Name, otpVerification.ExpiresAt);

        await _firebaseService.CreateOtpVerificationAsync(otpVerification);

        // Send OTP via email
        var emailSent = await _emailService.SendOtpEmailAsync(email ?? normalizedEmail, otpCode, fullName);
        
        if (emailSent)
        {
            _logger.LogInformation("OTP email sent successfully to {Email}", email);
        }
        else
        {
            _logger.LogWarning("Failed to send OTP email to {Email}, but OTP generated: {Otp}. Please configure email settings in appsettings.json", email, otpCode);
        }

        return otpCode;
    }

    public async Task<(bool Success, bool UserAlreadyExists)> VerifyOtpAndRegisterAsync(string email, string otpCode)
    {
        // Normalize email to lowercase for consistent comparison
        var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;
        var trimmedOtp = otpCode?.Trim() ?? string.Empty;
        
        _logger.LogInformation("Verifying OTP for Email={Email}, OTP={OtpCode}", normalizedEmail, trimmedOtp);
        
        var otpVerification = await _firebaseService.GetOtpVerificationAsync(normalizedEmail, trimmedOtp);

        if (otpVerification == null)
        {
            _logger.LogWarning("OTP verification failed: No matching OTP found for Email={Email}, OTP={OtpCode}", email, otpCode);
            return (false, false); // Wrong OTP
        }
        
        _logger.LogInformation("OTP verification found: Id={Id}, Email={Email}", otpVerification.Id, otpVerification.Email);

        // Check if user already exists (double check)
        if (await _firebaseService.UserExistsAsync(normalizedEmail))
        {
            await _firebaseService.MarkOtpAsUsedAsync(otpVerification.Id);
            return (false, true); // User already exists
        }

        // Create the user
        var user = new ApplicationUser
        {
            Email = normalizedEmail,
            PasswordHash = otpVerification.PasswordHash ?? string.Empty,
            FullName = otpVerification.FullName ?? string.Empty,
            IsAdmin = true, // All registered users are admin
            UserType = UserType.Admin,
            CreatedAt = DateTime.UtcNow,
            IsApproved = true
        };

        await _firebaseService.CreateUserAsync(user);

        // Mark OTP as used
        await _firebaseService.MarkOtpAsUsedAsync(otpVerification.Id);

        return (true, false); // Success
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        return await _firebaseService.UserExistsAsync(email);
    }

    public async Task<bool> RegisterAsync(string email, string password, string fullName)
    {
        // Normalize email to lowercase for consistent storage
        var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;
        
        _logger.LogInformation("=== DIRECT REGISTRATION (NO OTP) ===");
        _logger.LogInformation("Email: {Email} (Normalized: {NormalizedEmail}), FullName: {FullName}", 
            email, normalizedEmail, fullName);
        
        // Check if user already exists
        if (await _firebaseService.UserExistsAsync(normalizedEmail))
        {
            _logger.LogWarning("Registration failed: User already exists. Email: {Email}", normalizedEmail);
            return false;
        }

        var user = new ApplicationUser
        {
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = fullName,
            IsAdmin = true, // All registered users are admin
            UserType = UserType.Admin,
            CreatedAt = DateTime.UtcNow,
            IsApproved = true
        };

        await _firebaseService.CreateUserAsync(user);
        
        _logger.LogInformation("✓ User created successfully. Email: {Email}", normalizedEmail);

        return true;
    }

    public async Task LogoutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }

    public bool IsAuthenticated()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.User?.Identity?.IsAuthenticated ?? false;
    }

    public bool IsAdmin()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.User?.IsInRole("Admin") ?? false;
    }

    public string? GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public string? GetCurrentUserEmail()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
    }

    public string? GetCurrentUserType()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.User?.FindFirst("UserType")?.Value;
    }

    public bool RequiresPasswordChange()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.User?.FindFirst("RequiresPasswordChange")?.Value == "true";
    }

    public async Task<(bool Success, string? ErrorMessage)> SetPasswordAsync(string email, string newPassword)
    {
        try
        {
            // Normalize email
            var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;
            var user = await _firebaseService.GetUserByEmailAsync(normalizedEmail);

            if (user == null)
            {
                _logger.LogWarning("SetPassword failed: User not found for email: {Email}", email);
                return (false, "User not found. Please contact administrator.");
            }

            // Check if user is approved
            if (!user.IsApproved)
            {
                _logger.LogWarning("SetPassword failed: User not approved for email: {Email}", email);
                return (false, "Your account is not yet approved. Please contact administrator.");
            }

            // Hash the new password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);

            // Update user password and remove password change requirement
            user.PasswordHash = hashedPassword;
            user.RequiresPasswordChange = false;

            await _firebaseService.UpdateUserAsync(user.Id, user);

            _logger.LogInformation("Password set successfully for user: {Email}", normalizedEmail);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting password for email: {Email}", email);
            return (false, "An error occurred while setting your password. Please try again.");
        }
    }

    public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _firebaseService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            return false;
        }

        // Update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.RequiresPasswordChange = false; // Clear the flag after password change

        await _firebaseService.UpdateUserAsync(user.Id, user);

        // Update claims if user is logged in
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null && httpContext.User.Identity?.IsAuthenticated == true)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FullName", user.FullName),
                new Claim("UserType", user.UserType.ToString())
            };

            if (user.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        return true;
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        return await _firebaseService.GetUserByIdAsync(userId);
    }

    public async Task<bool> UpdateUserProfileAsync(string userId, string? profilePicturePath)
    {
        var user = await _firebaseService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        user.ProfilePicturePath = profilePicturePath;
        await _firebaseService.UpdateUserAsync(user.Id, user);

        return true;
    }
}
