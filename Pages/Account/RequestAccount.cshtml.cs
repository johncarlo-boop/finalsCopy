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

        // Check if user already exists
        if (await _firebaseService.UserExistsAsync(Input.Email))
        {
            ModelState.AddModelError(string.Empty, "An account with this email already exists.");
            return Page();
        }

        // Check if there's already a pending request
        if (await _firebaseService.AccountRequestExistsAsync(Input.Email))
        {
            ModelState.AddModelError(string.Empty, "You already have a pending account request. Please wait for admin approval.");
            return Page();
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
            
            // Return success immediately - don't wait for emails/notifications
            SuccessMessage = "Your account request has been submitted successfully. An admin will review your request and you will be notified once approved.";
            
            // Run all notifications in background (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
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
                    }
                    catch (Exception hubEx)
                    {
                        _logger.LogWarning(hubEx, "Failed to send SignalR notification for request {RequestId}", requestId);
                    }
                    
                    // Send confirmation email to requester (with timeout)
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                        await _emailService.SendAccountRequestConfirmationEmailAsync(Input.Email, Input.FullName)
                            .WaitAsync(cts.Token);
                        _logger.LogInformation("Account request confirmation email sent to {Email}", Input.Email);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("Confirmation email sending timed out for {Email}", Input.Email);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(emailEx, "Failed to send confirmation email to {Email}", Input.Email);
                    }
                    
                    // Send notification email to admins (with timeout)
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                        var adminUsers = await _firebaseService.GetAllAdminUsersAsync()
                            .WaitAsync(cts.Token);
                        var adminEmails = adminUsers?.Where(u => !string.IsNullOrWhiteSpace(u.Email))
                            .Select(u => u.Email).ToList() ?? new List<string>();
                        
                        if (adminEmails.Any())
                        {
                            using var emailCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                            await _emailService.SendNewAccountRequestNotificationToAdminsAsync(
                                Input.Email, 
                                Input.FullName, 
                                Input.Position, 
                                adminEmails
                            ).WaitAsync(emailCts.Token);
                            _logger.LogInformation("Account request notification sent to {Count} admin(s)", adminEmails.Count);
                        }
                        else
                        {
                            _logger.LogWarning("No admin emails found to send notification");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("Admin notification email sending timed out for request {RequestId}", requestId);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(emailEx, "Failed to send admin notification email for request {RequestId}", requestId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in background notification tasks for request {RequestId}", requestId);
                }
            });
            
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





