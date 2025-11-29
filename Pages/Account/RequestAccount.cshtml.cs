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
            
            SuccessMessage = "Your account request has been submitted successfully. An admin will review your request and you will be notified once approved.";
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





