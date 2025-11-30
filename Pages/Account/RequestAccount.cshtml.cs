using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using PropertyInventory.Hubs;
using PropertyInventory.Models;
using PropertyInventory.Services;

namespace PropertyInventory.Pages.Account;

public class RequestAccountModel : PageModel
{
    private readonly FirebaseService _firebaseService;
    private readonly EmailService _emailService;
    private readonly ILogger<RequestAccountModel> _logger;
    private readonly IHubContext<PropertyHub> _hubContext;

    public RequestAccountModel(
        FirebaseService firebaseService, 
        EmailService emailService, 
        ILogger<RequestAccountModel> logger,
        IHubContext<PropertyHub> hubContext)
    {
        _firebaseService = firebaseService;
        _emailService = emailService;
        _logger = logger;
        _hubContext = hubContext;
    }

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

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

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Skip validation entirely - return immediately and let Firebase handle duplicates
        // This makes the response instant - validation can happen in background if needed
        _logger.LogInformation("Skipping validation for {Email} - proceeding immediately (Firebase will validate)", Input.Email);

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
            // Return success IMMEDIATELY - don't wait for anything (TRULY INSTANT)
            SuccessMessage = "Your account request has been submitted successfully. An admin will review your request and you will be notified once approved.";
            
            // Capture variables before background task (for closure)
            var email = Input.Email;
            var fullName = Input.FullName;
            var position = Input.Position;
            
            // Start ALL operations in background IMMEDIATELY (fire-and-forget)
            // This ensures instant response - everything happens in background
            _ = Task.Run(async () =>
            {
                string requestId = string.Empty;
                bool createCompleted = false;
                
                try
                {
                    _logger.LogInformation("🚀 Starting background tasks for {Email}", email);
                    
                    // Create account request in background
                    try
                    {
                        _logger.LogInformation("Creating account request for {Email} in background", email);
                        var backgroundRequest = new AccountRequest
                        {
                            Email = email,
                            FullName = fullName,
                            Position = position,
                            Status = AccountRequestStatus.Pending,
                            RequestedAt = DateTime.UtcNow
                        };
                        
                        using var createCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                        requestId = await _firebaseService.CreateAccountRequestAsync(backgroundRequest)
                            .WaitAsync(createCts.Token);
                        backgroundRequest.Id = requestId;
                        createCompleted = true;
                        _logger.LogInformation("✓ Account request created successfully in background: {RequestId}", requestId);
                    }
                    catch (Exception createEx)
                    {
                        _logger.LogError(createEx, "✗ Failed to create account request in background for {Email}: {Error}", email, createEx.Message);
                        requestId = $"failed-{Guid.NewGuid():N}";
                        // Continue anyway - try to send email
                    }
                    
                    // Send SignalR notification (non-blocking)
                    if (createCompleted)
                    {
                        try
                        {
                            await _hubContext.Clients.All.SendAsync("AccountRequestCreated", new
                            {
                                requestId,
                                fullName,
                                email,
                                position,
                                requestedAt = DateTime.UtcNow
                            });
                            _logger.LogInformation("SignalR notification sent for request {RequestId}", requestId);
                        }
                        catch (Exception hubEx)
                        {
                            _logger.LogWarning(hubEx, "Failed to send SignalR notification for request {RequestId}", requestId);
                        }
                    }
                    
                    // Send confirmation email to requester ALWAYS (even if create is slow)
                    // This is the most important - user needs to know their request was received
                    try
                    {
                        _logger.LogInformation("📧 Attempting to send confirmation email to {Email}", email);
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                        var emailSent = await _emailService.SendAccountRequestConfirmationEmailAsync(email, fullName)
                            .WaitAsync(cts.Token);
                        
                        if (emailSent)
                        {
                            _logger.LogInformation("✓✓✓ Account request confirmation email sent successfully to {Email}", email);
                        }
                        else
                        {
                            _logger.LogError("✗✗✗ Confirmation email returned FALSE for {Email} - CHECK EMAIL SETTINGS!", email);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogError("✗✗✗ Confirmation email sending TIMED OUT for {Email} - CHECK EMAIL SETTINGS!", email);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "✗✗✗ FAILED to send confirmation email to {Email}: {Error}", email, emailEx.Message);
                    }
                    
                    // Send notification email to admins (with longer timeout for Render)
                    try
                    {
                        _logger.LogInformation("Fetching admin users for notification");
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                        var adminUsers = await _firebaseService.GetAllAdminUsersAsync()
                            .WaitAsync(cts.Token);
                        var adminEmails = adminUsers?.Where(u => !string.IsNullOrWhiteSpace(u.Email))
                            .Select(u => u.Email).ToList() ?? new List<string>();
                        
                        _logger.LogInformation("Found {Count} admin email(s)", adminEmails.Count);
                        
                        if (adminEmails.Any())
                        {
                            _logger.LogInformation("Attempting to send admin notification to {Count} admin(s)", adminEmails.Count);
                            using var emailCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                            var adminEmailSent = await _emailService.SendNewAccountRequestNotificationToAdminsAsync(
                                email, 
                                fullName, 
                                position, 
                                adminEmails
                            ).WaitAsync(emailCts.Token);
                            
                            if (adminEmailSent)
                            {
                                _logger.LogInformation("✓ Account request notification sent successfully to {Count} admin(s)", adminEmails.Count);
                            }
                            else
                            {
                                _logger.LogWarning("✗ Admin notification email returned false");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("No admin emails found to send notification");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("✗ Admin notification email sending timed out for request {RequestId}", requestId);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "✗ Failed to send admin notification email for request {RequestId}: {Error}", requestId, emailEx.Message);
                    }
                    
                    _logger.LogInformation("Background email tasks completed for request {RequestId}", requestId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "✗ Error in background notification tasks for request {RequestId}: {Error}", requestId, ex.Message);
                }
            });
            
            // Return IMMEDIATELY - don't wait for background tasks
            return RedirectToPage("./RequestAccount");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account request");
            ErrorMessage = "An error occurred while submitting your request. Please try again.";
            return Page();
        }
    }
}





