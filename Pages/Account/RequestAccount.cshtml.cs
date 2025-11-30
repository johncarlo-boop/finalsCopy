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

        // Run validation checks in parallel with SHORT timeout (max 3 seconds for fast response)
        // If timeout, proceed anyway - Firebase will catch duplicates
        try
        {
            using var validationCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            
            var userExistsTask = _firebaseService.UserExistsAsync(Input.Email);
            var requestExistsTask = _firebaseService.AccountRequestExistsAsync(Input.Email);
            
            // Wait for both checks to complete or timeout (max 3 seconds)
            await Task.WhenAll(userExistsTask, requestExistsTask).WaitAsync(validationCts.Token);
            
            // Check results only if completed in time
            if (await userExistsTask)
            {
                ModelState.AddModelError(string.Empty, "An account with this email already exists.");
                return Page();
            }

            if (await requestExistsTask)
            {
                ModelState.AddModelError(string.Empty, "You already have a pending account request. Please wait for admin approval.");
                return Page();
            }
        }
        catch (OperationCanceledException)
        {
            // If validation times out (after 3 seconds), proceed immediately
            // Firebase will catch duplicates on create - this is fine
            _logger.LogInformation("Validation checks timed out for {Email} (proceeding anyway - Firebase will validate)", Input.Email);
        }
        catch (Exception validationEx)
        {
            // If validation fails, proceed anyway
            _logger.LogInformation(validationEx, "Validation checks failed for {Email} (proceeding anyway)", Input.Email);
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
            // Create request with timeout protection
            string requestId;
            try
            {
                using var createCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                requestId = await _firebaseService.CreateAccountRequestAsync(request)
                    .WaitAsync(createCts.Token);
                request.Id = requestId;
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("CreateAccountRequestAsync timed out for {Email}", Input.Email);
                ErrorMessage = "Request creation timed out. Please try again.";
                return Page();
            }
            
            // Return success IMMEDIATELY - don't wait for anything
            SuccessMessage = "Your account request has been submitted successfully. An admin will review your request and you will be notified once approved.";
            
            // Capture variables before background task (for closure)
            var email = Input.Email;
            var fullName = Input.FullName;
            var position = Input.Position;
            
            // Start background email task IMMEDIATELY (fire-and-forget)
            // Use Task.Run with ConfigureAwait(false) for better performance
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Starting background email tasks for request {RequestId}", requestId);
                    
                    // Send SignalR notification (non-blocking)
                    try
                    {
                        await _hubContext.Clients.All.SendAsync("AccountRequestCreated", new
                        {
                            requestId,
                            request.FullName,
                            request.Email,
                            request.Position,
                            request.RequestedAt
                        });
                        _logger.LogInformation("SignalR notification sent for request {RequestId}", requestId);
                    }
                    catch (Exception hubEx)
                    {
                        _logger.LogWarning(hubEx, "Failed to send SignalR notification for request {RequestId}", requestId);
                    }
                    
                    // Send confirmation email to requester (with longer timeout for Render)
                    try
                    {
                        _logger.LogInformation("Attempting to send confirmation email to {Email}", email);
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                        var emailSent = await _emailService.SendAccountRequestConfirmationEmailAsync(email, fullName)
                            .WaitAsync(cts.Token);
                        
                        if (emailSent)
                        {
                            _logger.LogInformation("✓ Account request confirmation email sent successfully to {Email}", email);
                        }
                        else
                        {
                            _logger.LogWarning("✗ Confirmation email returned false for {Email}", email);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("✗ Confirmation email sending timed out for {Email}", email);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "✗ Failed to send confirmation email to {Email}: {Error}", email, emailEx.Message);
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





