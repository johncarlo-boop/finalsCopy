using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using PropertyInventory.Hubs;
using PropertyInventory.Models;
using PropertyInventory.Services;

namespace PropertyInventory.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AccountRequestsModel : PageModel
{
    private readonly FirebaseService _firebaseService;
    private readonly EmailService _emailService;
    private readonly ILogger<AccountRequestsModel> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHubContext<PropertyHub> _hubContext;

    public AccountRequestsModel(
        FirebaseService firebaseService, 
        EmailService emailService, 
        ILogger<AccountRequestsModel> logger, 
        IConfiguration configuration,
        IHubContext<PropertyHub> hubContext)
    {
        _firebaseService = firebaseService;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
        _hubContext = hubContext;
    }

    public List<AccountRequest> AccountRequests { get; set; } = new();
    public List<AccountRequest> PendingRequests { get; private set; } = new();
    public List<AccountRequest> ApprovedRequests { get; private set; } = new();
    public List<AccountRequest> RejectedRequests { get; private set; } = new();
    
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int PendingCount { get; set; }
    public int TotalRequests => AccountRequests?.Count ?? 0;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Position")]
        public string? Position { get; set; }
    }

    public async Task OnGetAsync()
    {
        // Get all account requests directly from database - only show what exists in database
        var allRequests = await _firebaseService.GetAllAccountRequestsAsync();
        
        // Only show account requests that exist in the database
        // No additional filtering - just what's in the database
        AccountRequests = allRequests.OrderByDescending(r => r.RequestedAt).ToList();
        PendingRequests = AccountRequests.Where(r => r.Status == AccountRequestStatus.Pending).ToList();
        ApprovedRequests = AccountRequests.Where(r => r.Status == AccountRequestStatus.Approved).ToList();
        RejectedRequests = AccountRequests.Where(r => r.Status == AccountRequestStatus.Rejected).ToList();
        
        // Get counts directly from database
        var counts = await _firebaseService.GetAccountRequestCountsAsync();
        ApprovedCount = counts.Approved;
        RejectedCount = counts.Rejected;
        PendingCount = counts.Pending;
    }

    public async Task<IActionResult> OnPostRequestAccountAsync()
    {
        if (!ModelState.IsValid)
        {
            ErrorMessage = "Please fill in all required fields.";
            return RedirectToPage();
        }

        // Check if user already exists
        if (await _firebaseService.UserExistsAsync(Input.Email))
        {
            ErrorMessage = "An account with this email already exists.";
            return RedirectToPage();
        }

        // Check if there's already a pending request
        if (await _firebaseService.AccountRequestExistsAsync(Input.Email))
        {
            ErrorMessage = "There is already a pending account request for this email.";
            return RedirectToPage();
        }

        // Create account request
        var request = new AccountRequest
        {
            Email = Input.Email,
            FullName = Input.FullName,
            Position = Input.Position,
            Status = AccountRequestStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        try
        {
            var requestId = await _firebaseService.CreateAccountRequestAsync(request);
            request.Id = requestId;
            
            await _hubContext.Clients.All.SendAsync("AccountRequestCreated", new
            {
                requestId,
                request.FullName,
                request.Email,
                request.Position,
                request.RequestedAt
            });
            
            // Send confirmation email to requester
            try
            {
                await _emailService.SendAccountRequestConfirmationEmailAsync(Input.Email, Input.FullName);
                _logger.LogInformation("Account request confirmation email sent to {Email}", Input.Email);
            }
            catch (Exception emailEx)
            {
                _logger.LogWarning(emailEx, "Failed to send confirmation email to {Email}, but request was created", Input.Email);
            }
            
            // Send notification email to admins
            try
            {
                var adminUsers = await _firebaseService.GetAllAdminUsersAsync();
                var adminEmails = adminUsers.Where(u => !string.IsNullOrWhiteSpace(u.Email)).Select(u => u.Email).ToList();
                
                if (adminEmails.Any())
                {
                    await _emailService.SendNewAccountRequestNotificationToAdminsAsync(
                        Input.Email, 
                        Input.FullName, 
                        Input.Position, 
                        adminEmails
                    );
                    _logger.LogInformation("Account request notification sent to {Count} admin(s)", adminEmails.Count);
                }
                else
                {
                    _logger.LogWarning("No admin emails found to send notification");
                }
            }
            catch (Exception emailEx)
            {
                _logger.LogWarning(emailEx, "Failed to send admin notification email, but request was created");
            }
            
            SuccessMessage = $"Account request for {Input.FullName} ({Input.Email}) has been submitted successfully.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account request");
            ErrorMessage = "An error occurred while submitting the request. Please try again.";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostAsync(string action, string requestId, string? rejectionReason = null)
    {
        if (string.IsNullOrEmpty(requestId))
        {
            ErrorMessage = "Invalid request ID.";
            return RedirectToPage();
        }

        var request = await _firebaseService.GetAccountRequestByIdAsync(requestId);
        if (request == null)
        {
            ErrorMessage = "Account request not found.";
            return RedirectToPage();
        }

        if (action == "approve")
        {
            // Generate a temporary password for the user
            var temporaryPassword = GenerateTemporaryPassword();
            
            var user = new ApplicationUser
            {
                Email = request.Email,
                FullName = request.FullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
                IsAdmin = false,
                UserType = UserType.MobileUser,
                CreatedAt = DateTime.UtcNow,
                IsApproved = true,
                RequiresPasswordChange = false // User can login with temporary password and change it later
            };

            try
            {
                await _firebaseService.CreateUserAsync(user);

                // Update request status
                request.Status = AccountRequestStatus.Approved;
                request.ReviewedAt = DateTime.UtcNow;
                request.ReviewedBy = User.Identity?.Name;
                await _firebaseService.UpdateAccountRequestAsync(requestId, request);

                // Build login URL - MUST be absolute URL for Gmail mobile
                // Priority: 1. AppSettings:BaseUrl, 2. Request URL, 3. Fallback
                string loginUrl;
                var baseUrl = _configuration["AppSettings:BaseUrl"];
                
                if (!string.IsNullOrEmpty(baseUrl))
                {
                    // Use configured base URL - point to MobileLogin page
                    loginUrl = $"{baseUrl.TrimEnd('/')}/Account/MobileLogin";
                    _logger.LogInformation("Using configured BaseUrl: {BaseUrl}", baseUrl);
                }
                else
                {
                    // Fallback to request URL
                    var scheme = Request.IsHttps ? "https" : "http";
                    var host = Request.Host.Value;
                    loginUrl = $"{scheme}://{host}/Account/MobileLogin";
                    _logger.LogInformation("Using request URL: {Scheme}://{Host}", scheme, host);
                }
                
                // Ensure URL is absolute
                if (!loginUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                    !loginUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    loginUrl = $"http://{loginUrl}";
                }
                
                _logger.LogInformation("Sending approval email to {Email} with login URL: {LoginUrl} and temporary password", request.Email, loginUrl);
                
                // Send approval email with temporary password and login link
                await _emailService.SendAccountApprovalEmailAsync(request.Email, temporaryPassword, request.FullName, loginUrl);

                SuccessMessage = $"Account approved for {request.Email}. Temporary password and login link sent via email.";

                await _hubContext.Clients.All.SendAsync("AccountRequestStatusChanged", new
                {
                    requestId,
                    status = request.Status.ToString(),
                    request.FullName,
                    request.Email,
                    reviewedBy = request.ReviewedBy ?? "Admin",
                    request.ReviewedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving account request");
                ErrorMessage = "An error occurred while approving the account request.";
            }
        }
        else if (action == "reject")
        {
            // Update request status
            request.Status = AccountRequestStatus.Rejected;
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewedBy = User.Identity?.Name;
            request.RejectionReason = rejectionReason;
            await _firebaseService.UpdateAccountRequestAsync(requestId, request);

            // Send rejection email
            await _emailService.SendAccountRejectionEmailAsync(request.Email, request.FullName, rejectionReason);

            SuccessMessage = $"Account request rejected for {request.Email}.";

            await _hubContext.Clients.All.SendAsync("AccountRequestStatusChanged", new
            {
                requestId,
                status = request.Status.ToString(),
                request.FullName,
                request.Email,
                reviewedBy = request.ReviewedBy ?? "Admin",
                request.ReviewedAt,
                request.RejectionReason
            });
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                ErrorMessage = "Invalid request ID.";
                return RedirectToPage();
            }

            var request = await _firebaseService.GetAccountRequestByIdAsync(id);
            if (request == null)
            {
                ErrorMessage = "Account request not found.";
                return RedirectToPage();
            }

            await _firebaseService.DeleteAccountRequestAsync(id);
            
            SuccessMessage = $"Account request for {request.Email} has been deleted successfully.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account request");
            ErrorMessage = $"An error occurred while deleting the account request: {ex.Message}";
            return RedirectToPage();
        }
    }

    private string GenerateTemporaryPassword()
    {
        // Generate a random 8-character password with uppercase, lowercase, and numbers
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}





