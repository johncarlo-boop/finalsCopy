using System.Net;
using System.Net.Mail;

namespace PropertyInventory.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendOtpEmailAsync(string toEmail, string otpCode, string fullName)
    {
        try
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUsername;
            var fromName = _configuration["EmailSettings:FromName"] ?? "Property Inventory System";

            if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("Email settings not configured. OTP: {Otp}", otpCode);
                return false;
            }

            using var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(fromEmail ?? smtpUsername ?? "noreply@example.com", fromName);
            mailMessage.To.Add(toEmail);
            mailMessage.Subject = "Admin Account Verification - OTP Code";
            mailMessage.Body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #006400; color: white; padding: 20px; text-align: center; }}
        .header img {{ max-width: 120px; height: auto; margin-bottom: 15px; display: block; margin-left: auto; margin-right: auto; }}
        .content {{ background-color: #f9f9f9; padding: 30px; }}
        .otp-box {{ background-color: #006400; color: white; font-size: 32px; font-weight: bold; 
                     text-align: center; padding: 20px; margin: 20px 0; letter-spacing: 5px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <img src=""https://upload.wikimedia.org/wikipedia/en/b/b6/The_Colegio_de_Montalban_Seal.png"" alt=""Colegio de Montalban Logo"" />
            <h2>Colegio de Montalban</h2>
            <p>Property Inventory Management System</p>
        </div>
        <div class=""content"">
            <h3>Hello {fullName},</h3>
            <p>Thank you for registering as an admin. Please use the following OTP code to complete your registration:</p>
            <div class=""otp-box"">{otpCode}</div>
            <p><strong>This OTP will expire in 10 minutes.</strong></p>
            <p>If you did not request this code, please ignore this email.</p>
        </div>
        <div class=""footer"">
            <p>This is an automated message. Please do not reply.</p>
        </div>
    </div>
</body>
</html>";
            mailMessage.IsBodyHtml = true;

            using var smtpClient = new SmtpClient(smtpServer, smtpPort);
            smtpClient.EnableSsl = true;
            smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("OTP email sent successfully to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendTemporaryPasswordEmailAsync(string toEmail, string temporaryPassword, string fullName)
    {
        try
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUsername;
            var fromName = _configuration["EmailSettings:FromName"] ?? "Property Inventory System";

            if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("Email settings not configured. Temporary Password: {Password}", temporaryPassword);
                return false;
            }

            using var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(fromEmail ?? smtpUsername ?? "noreply@example.com", fromName);
            mailMessage.To.Add(toEmail);
            mailMessage.Subject = "Password Reset - Temporary Password";
            mailMessage.Body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #006400; color: white; padding: 20px; text-align: center; }}
        .header img {{ max-width: 120px; height: auto; margin-bottom: 15px; display: block; margin-left: auto; margin-right: auto; }}
        .content {{ background-color: #f9f9f9; padding: 30px; }}
        .password-box {{ background-color: #006400; color: white; font-size: 24px; font-weight: bold; 
                     text-align: center; padding: 20px; margin: 20px 0; letter-spacing: 3px; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <img src=""https://upload.wikimedia.org/wikipedia/en/b/b6/The_Colegio_de_Montalban_Seal.png"" alt=""Colegio de Montalban Logo"" />
            <h2>Colegio de Montalban</h2>
            <p>Property Inventory Management System</p>
        </div>
        <div class=""content"">
            <h3>Hello {fullName},</h3>
            <p>You have requested to reset your password. Please use the following temporary password to login:</p>
            <div class=""password-box"">{temporaryPassword}</div>
            <div class=""warning"">
                <strong>⚠️ Important:</strong> You will be required to change this password immediately after logging in for security purposes.
            </div>
            <p>If you did not request this password reset, please ignore this message.</p>
        </div>
        <div class=""footer"">
            <p>This is an automated message. Please do not reply.</p>
        </div>
    </div>
</body>
</html>";
            mailMessage.IsBodyHtml = true;

            using var smtpClient = new SmtpClient(smtpServer, smtpPort);
            smtpClient.EnableSsl = true;
            smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Temporary password email sent successfully to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send temporary password email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendAccountApprovalEmailAsync(string toEmail, string? temporaryPassword, string fullName, string? loginUrl = null)
    {
        try
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUsername;
            var fromName = _configuration["EmailSettings:FromName"] ?? "Property Inventory System";
            var baseUrl = _configuration["AppSettings:BaseUrl"];

            if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("Email settings not configured. Temporary Password: {Password}", temporaryPassword);
                return false;
            }

            // Build login URL - MUST be absolute URL for Gmail mobile
            // Point to SetPassword page with email parameter
            if (string.IsNullOrEmpty(loginUrl))
            {
                if (!string.IsNullOrEmpty(baseUrl))
                {
                    var encodedEmail = Uri.EscapeDataString(toEmail);
                    loginUrl = $"{baseUrl.TrimEnd('/')}/Account/SetPassword?email={encodedEmail}";
                }
                else
                {
                    // Fallback - but this won't work in email, so log warning
                    _logger.LogWarning("No base URL configured and no loginUrl provided. Link may not work in email.");
                    var encodedEmail = Uri.EscapeDataString(toEmail);
                    loginUrl = $"https://your-domain.com/Account/SetPassword?email={encodedEmail}"; // Placeholder - should be configured
                }
            }
            else
            {
                // If loginUrl is provided but doesn't have email parameter, add it
                if (!loginUrl.Contains("email="))
                {
                    var encodedEmail = Uri.EscapeDataString(toEmail);
                    var separator = loginUrl.Contains("?") ? "&" : "?";
                    loginUrl = $"{loginUrl}{separator}email={encodedEmail}";
                }
            }
            
            // Ensure URL is absolute (starts with http:// or https://)
            if (!loginUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                !loginUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // If relative URL, try to make it absolute using baseUrl
                if (!string.IsNullOrEmpty(baseUrl))
                {
                    var baseUrlTrimmed = baseUrl.TrimEnd('/');
                    loginUrl = $"{baseUrlTrimmed}{loginUrl}";
                }
                else
                {
                    _logger.LogWarning("Login URL is relative and no base URL configured: {LoginUrl}", loginUrl);
                }
            }

            using var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(fromEmail ?? smtpUsername ?? "noreply@example.com", fromName);
            mailMessage.To.Add(toEmail);
            mailMessage.Subject = "Account Request Approved - Login Information";
            mailMessage.Body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #006400; color: white; padding: 20px; text-align: center; }}
        .header img {{ max-width: 120px; height: auto; margin-bottom: 15px; display: block; margin-left: auto; margin-right: auto; }}
        .content {{ background-color: #f9f9f9; padding: 30px; }}
        .password-box {{ background-color: #006400; color: white; font-size: 24px; font-weight: bold; 
                     text-align: center; padding: 20px; margin: 20px 0; letter-spacing: 3px; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        .login-button {{ display: inline-block; background-color: #006400; color: white !important; padding: 15px 30px; 
                        text-decoration: none !important; border-radius: 5px; font-weight: bold; margin: 20px 0; 
                        text-align: center; font-size: 16px; width: auto; min-width: 200px; }}
        .login-button:hover {{ background-color: #004d00; }}
        @media only screen and (max-width: 600px) {{
            .login-button {{ padding: 18px 35px; font-size: 18px; display: block; width: 100%; }}
        }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
        .credentials-box {{ background-color: #e8f5e9; border: 2px solid #006400; padding: 20px; margin: 20px 0; 
                           border-radius: 5px; }}
        .credentials-box p {{ margin: 5px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <img src=""https://upload.wikimedia.org/wikipedia/en/b/b6/The_Colegio_de_Montalban_Seal.png"" alt=""Colegio de Montalban Logo"" />
            <h2>Colegio de Montalban</h2>
            <p>Property Inventory Management System</p>
        </div>
        <div class=""content"">
            <h3>Hello {fullName},</h3>
            <p>Your account request has been approved! You can now access the Property Inventory Management System.</p>
            
            <div class=""credentials-box"">
                <h4 style=""margin-top: 0; color: #006400;"">Your Account Information:</h4>
                <p><strong>Email:</strong> {toEmail}</p>
                {(string.IsNullOrEmpty(temporaryPassword) ? "" : $@"
                <p><strong>Temporary Password:</strong></p>
                <div class=""password-box"">{temporaryPassword}</div>
                <p style=""text-align: center; color: #666; font-size: 14px;""><em>Please change this password after logging in for security.</em></p>
                ")}
            </div>
            
            <div style=""text-align: center; margin: 30px 0;"">
                <a href=""{loginUrl}"" class=""login-button"" style=""display: inline-block; background-color: #006400; color: white !important; padding: 15px 30px; text-decoration: none !important; border-radius: 5px; font-weight: bold; margin: 20px 0; text-align: center; font-size: 16px;"">Click Here to Login</a>
            </div>
            <p style=""text-align: center; margin-top: 15px; color: #666; font-size: 14px;"">
                Or copy and paste this link in your browser:<br/>
                <a href=""{loginUrl}"" style=""color: #006400; word-break: break-all;"">{loginUrl}</a>
            </p>
            
            <div class=""warning"">
                <strong>⚠️ Important:</strong> 
                <ul style=""margin: 10px 0; padding-left: 20px;"">
                    {(string.IsNullOrEmpty(temporaryPassword) ? @"
                    <li>Click the login button above to access the mobile login page.</li>
                    <li>If this is your first login, you will be asked to set your password.</li>
                    " : @"
                    <li>Use the temporary password above to login for the first time.</li>
                    <li>After logging in, please change your password in your profile settings.</li>
                    ")}
                    <li>Please choose a strong password (at least 6 characters).</li>
                    <li>Please keep your login credentials secure and do not share them with anyone.</li>
                    <li>If you did not request this account, please contact the administrator immediately.</li>
                </ul>
            </div>
            
            <p style=""margin-top: 20px;"">After logging in, you can use the QR code scanner to edit properties.</p>
        </div>
        <div class=""footer"">
            <p>This is an automated message. Please do not reply.</p>
            <p>If you have any questions, please contact the administrator.</p>
        </div>
    </div>
</body>
</html>";
            mailMessage.IsBodyHtml = true;

            using var smtpClient = new SmtpClient(smtpServer, smtpPort);
            smtpClient.EnableSsl = true;
            smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Account approval email sent successfully to {Email} with login link: {LoginUrl}", toEmail, loginUrl);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send account approval email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendAccountRequestConfirmationEmailAsync(string toEmail, string fullName)
    {
        try
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUsername;
            var fromName = _configuration["EmailSettings:FromName"] ?? "Property Inventory System";
            var baseUrl = _configuration["AppSettings:BaseUrl"];

            if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("Email settings not configured. Cannot send account request confirmation email.");
                return false;
            }

            // Build login URL for reference (will be used after approval)
            string loginUrl = "";
            if (!string.IsNullOrEmpty(baseUrl))
            {
                loginUrl = $"{baseUrl.TrimEnd('/')}/Account/MobileLogin";
            }
            else
            {
                loginUrl = "https://finalscopy-pdiw.onrender.com/Account/MobileLogin";
            }

            using var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(fromEmail ?? smtpUsername ?? "noreply@example.com", fromName);
            mailMessage.To.Add(toEmail);
            mailMessage.Subject = "Account Request Received - Colegio de Montalban";
            mailMessage.Body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #006400; color: white; padding: 20px; text-align: center; }}
        .header img {{ max-width: 120px; height: auto; margin-bottom: 15px; display: block; margin-left: auto; margin-right: auto; }}
        .content {{ background-color: #f9f9f9; padding: 30px; }}
        .info-box {{ background-color: #e8f5e9; border: 2px solid #006400; padding: 20px; margin: 20px 0; 
                           border-radius: 5px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <img src=""https://upload.wikimedia.org/wikipedia/en/b/b6/The_Colegio_de_Montalban_Seal.png"" alt=""Colegio de Montalban Logo"" />
            <h2>Colegio de Montalban</h2>
            <p>Property Inventory Management System</p>
        </div>
        <div class=""content"">
            <h3>Hello {fullName},</h3>
            <p>Thank you for requesting an account with the Property Inventory Management System.</p>
            
            <div class=""info-box"">
                <h4 style=""margin-top: 0; color: #006400;"">Your Request Has Been Received</h4>
                <p><strong>Email:</strong> {toEmail}</p>
                <p><strong>Status:</strong> <span style=""color: #ff9800; font-weight: bold;"">Pending Review</span></p>
            </div>
            
            <p>Your account request is now under review by our administrators. You will receive an email notification once your request has been approved or rejected.</p>
            
            <p><strong>What happens next?</strong></p>
            <ul>
                <li>An administrator will review your request</li>
                <li>You will receive an email notification with the decision</li>
                <li>If approved, you will receive login credentials via email</li>
                <li>You will be able to access the system at: <a href=""{loginUrl}"" style=""color: #006400;"">{loginUrl}</a></li>
            </ul>
            
            <p><strong>System Access:</strong></p>
            <p>Once your account is approved, you can access the Property Inventory Management System at:</p>
            <div style=""text-align: center; margin: 20px 0;"">
                <a href=""{loginUrl}"" style=""display: inline-block; background-color: #006400; color: white !important; padding: 12px 24px; text-decoration: none !important; border-radius: 5px; font-weight: bold;"">Access System</a>
            </div>
            <p style=""text-align: center; color: #666; font-size: 14px;"">(Link will be active after account approval)</p>
            
            <p>If you have any questions, please contact the administrator.</p>
        </div>
        <div class=""footer"">
            <p>This is an automated message. Please do not reply.</p>
        </div>
    </div>
</body>
</html>";
            mailMessage.IsBodyHtml = true;

            using var smtpClient = new SmtpClient(smtpServer, smtpPort);
            smtpClient.EnableSsl = true;
            smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Account request confirmation email sent successfully to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send account request confirmation email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendNewAccountRequestNotificationToAdminsAsync(string requesterEmail, string requesterName, string? position, List<string> adminEmails)
    {
        try
        {
            if (adminEmails == null || !adminEmails.Any())
            {
                _logger.LogWarning("No admin emails provided for notification");
                return false;
            }

            var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUsername;
            var fromName = _configuration["EmailSettings:FromName"] ?? "Property Inventory System";

            if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("Email settings not configured. Cannot send admin notification.");
                return false;
            }

            // Build admin URL - try to get from config or use fallback
            string adminUrl;
            var baseUrl = _configuration["AppSettings:BaseUrl"];
            if (!string.IsNullOrEmpty(baseUrl))
            {
                adminUrl = $"{baseUrl.TrimEnd('/')}/Admin/AccountRequests";
            }
            else
            {
                // Fallback
                adminUrl = "http://localhost:5000/Admin/AccountRequests";
            }

            using var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(fromEmail ?? smtpUsername ?? "noreply@example.com", fromName);
            
            // Add all admin emails
            foreach (var adminEmail in adminEmails)
            {
                if (!string.IsNullOrWhiteSpace(adminEmail))
                {
                    mailMessage.To.Add(adminEmail);
                }
            }

            if (mailMessage.To.Count == 0)
            {
                _logger.LogWarning("No valid admin emails to send notification to");
                return false;
            }

            mailMessage.Subject = $"New Account Request - {requesterName}";
            mailMessage.Body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #006400; color: white; padding: 20px; text-align: center; }}
        .header img {{ max-width: 120px; height: auto; margin-bottom: 15px; display: block; margin-left: auto; margin-right: auto; }}
        .content {{ background-color: #f9f9f9; padding: 30px; }}
        .request-box {{ background-color: #fff3cd; border: 2px solid #ffc107; padding: 20px; margin: 20px 0; 
                           border-radius: 5px; }}
        .button {{ display: inline-block; background-color: #006400; color: white; padding: 15px 30px; 
                        text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; 
                        text-align: center; font-size: 16px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <img src=""https://upload.wikimedia.org/wikipedia/en/b/b6/The_Colegio_de_Montalban_Seal.png"" alt=""Colegio de Montalban Logo"" />
            <h2>Colegio de Montalban</h2>
            <p>Property Inventory Management System</p>
        </div>
        <div class=""content"">
            <h3>New Account Request</h3>
            <p>A new account request has been submitted and requires your review.</p>
            
            <div class=""request-box"">
                <h4 style=""margin-top: 0; color: #856404;"">Request Details</h4>
                <p><strong>Full Name:</strong> {requesterName}</p>
                <p><strong>Email:</strong> {requesterEmail}</p>
                <p><strong>Position:</strong> {(string.IsNullOrEmpty(position) ? "N/A" : position)}</p>
                <p><strong>Requested At:</strong> {DateTime.UtcNow.ToString("MMM dd, yyyy HH:mm")} UTC</p>
            </div>
            
            <div style=""text-align: center; margin: 30px 0;"">
                <a href=""{adminUrl}"" class=""button"">Review Account Request</a>
            </div>
            
            <p>Please log in to the admin dashboard to approve or reject this request.</p>
        </div>
        <div class=""footer"">
            <p>This is an automated message. Please do not reply.</p>
        </div>
    </div>
</body>
</html>";
            mailMessage.IsBodyHtml = true;

            using var smtpClient = new SmtpClient(smtpServer, smtpPort);
            smtpClient.EnableSsl = true;
            smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("New account request notification sent to {Count} admin(s)", mailMessage.To.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send new account request notification to admins");
            return false;
        }
    }

    public async Task<bool> SendAccountRejectionEmailAsync(string toEmail, string fullName, string? rejectionReason)
    {
        try
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUsername;
            var fromName = _configuration["EmailSettings:FromName"] ?? "Property Inventory System";

            if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("Email settings not configured. Cannot send rejection email.");
                return false;
            }

            using var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(fromEmail ?? smtpUsername ?? "noreply@example.com", fromName);
            mailMessage.To.Add(toEmail);
            mailMessage.Subject = "Account Request Rejected";
            mailMessage.Body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 30px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h2>Colegio de Montalban</h2>
            <p>Property Inventory Management System</p>
        </div>
        <div class=""content"">
            <h3>Hello {fullName},</h3>
            <p>We regret to inform you that your account request has been rejected.</p>
            {(string.IsNullOrEmpty(rejectionReason) ? "" : $"<p><strong>Reason:</strong> {rejectionReason}</p>")}
            <p>If you have any questions, please contact the administrator.</p>
        </div>
        <div class=""footer"">
            <p>This is an automated message. Please do not reply.</p>
        </div>
    </div>
</body>
</html>";
            mailMessage.IsBodyHtml = true;

            using var smtpClient = new SmtpClient(smtpServer, smtpPort);
            smtpClient.EnableSsl = true;
            smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Account rejection email sent successfully to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send account rejection email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendOverduePropertyNotificationToAdminsAsync(List<PropertyInventory.Models.Property> overdueProperties, List<string> adminEmails)
    {
        try
        {
            if (overdueProperties == null || !overdueProperties.Any())
            {
                _logger.LogInformation("No overdue properties to notify about");
                return true;
            }

            if (adminEmails == null || !adminEmails.Any())
            {
                _logger.LogWarning("No admin emails provided for overdue property notification");
                return false;
            }

            var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUsername;
            var fromName = _configuration["EmailSettings:FromName"] ?? "Property Inventory System";
            var baseUrl = _configuration["AppSettings:BaseUrl"];

            if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("Email settings not configured. Cannot send overdue property notification.");
                return false;
            }

            // Build properties list HTML
            var propertiesListHtml = new System.Text.StringBuilder();
            foreach (var property in overdueProperties)
            {
                var daysOverdue = (DateTime.UtcNow - property.ReturnDate!.Value.ToUniversalTime()).Days;
                var returnDateStr = property.ReturnDate.Value.ToLocalTime().ToString("MMM dd, yyyy");
                
                propertiesListHtml.Append($@"
                <tr>
                    <td style=""padding: 10px; border-bottom: 1px solid #ddd;"">{property.PropertyCode}</td>
                    <td style=""padding: 10px; border-bottom: 1px solid #ddd;"">{property.PropertyName}</td>
                    <td style=""padding: 10px; border-bottom: 1px solid #ddd;"">{property.BorrowerName}</td>
                    <td style=""padding: 10px; border-bottom: 1px solid #ddd;"">{returnDateStr}</td>
                    <td style=""padding: 10px; border-bottom: 1px solid #ddd; color: #dc3545; font-weight: bold;"">{daysOverdue} day(s)</td>
                </tr>");
            }

            // Build admin URL
            string adminUrl;
            if (!string.IsNullOrEmpty(baseUrl))
            {
                adminUrl = $"{baseUrl.TrimEnd('/')}/Index";
            }
            else
            {
                adminUrl = "http://localhost:5000/Index";
            }

            using var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(fromEmail ?? smtpUsername ?? "noreply@example.com", fromName);
            
            // Add all admin emails
            foreach (var adminEmail in adminEmails)
            {
                if (!string.IsNullOrWhiteSpace(adminEmail))
                {
                    mailMessage.To.Add(adminEmail);
                }
            }

            if (mailMessage.To.Count == 0)
            {
                _logger.LogWarning("No valid admin emails to send overdue property notification to");
                return false;
            }

            mailMessage.Subject = $"⚠️ Overdue Property Alert - {overdueProperties.Count} Property(ies) Not Returned";
            mailMessage.Body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 800px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
        .header img {{ max-width: 120px; height: auto; margin-bottom: 15px; display: block; margin-left: auto; margin-right: auto; }}
        .content {{ background-color: #f9f9f9; padding: 30px; }}
        .alert-box {{ background-color: #fff3cd; border: 2px solid #ffc107; padding: 20px; margin: 20px 0; border-radius: 5px; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; background-color: white; }}
        th {{ background-color: #006400; color: white; padding: 12px; text-align: left; }}
        td {{ padding: 10px; border-bottom: 1px solid #ddd; }}
        .button {{ display: inline-block; background-color: #006400; color: white; padding: 15px 30px; 
                        text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; 
                        text-align: center; font-size: 16px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <img src=""https://upload.wikimedia.org/wikipedia/en/b/b6/The_Colegio_de_Montalban_Seal.png"" alt=""Colegio de Montalban Logo"" />
            <h2>Colegio de Montalban</h2>
            <p>Property Inventory Management System</p>
        </div>
        <div class=""content"">
            <h3>⚠️ Overdue Property Alert</h3>
            <p>The following {(overdueProperties.Count == 1 ? "property has" : "properties have")} not been returned on time:</p>
            
            <div class=""alert-box"">
                <strong>⚠️ Action Required:</strong> Please follow up with the borrower(s) to ensure the property(ies) are returned.
            </div>
            
            <table>
                <thead>
                    <tr>
                        <th>Property Code</th>
                        <th>Property Name</th>
                        <th>Borrower Name</th>
                        <th>Expected Return Date</th>
                        <th>Days Overdue</th>
                    </tr>
                </thead>
                <tbody>
                    {propertiesListHtml}
                </tbody>
            </table>
            
            <div style=""text-align: center; margin: 30px 0;"">
                <a href=""{adminUrl}"" class=""button"">View Properties</a>
            </div>
            
            <p>This is an automated notification. The system will continue to check for overdue properties daily.</p>
        </div>
        <div class=""footer"">
            <p>This is an automated message. Please do not reply.</p>
        </div>
    </div>
</body>
</html>";
            mailMessage.IsBodyHtml = true;

            using var smtpClient = new SmtpClient(smtpServer, smtpPort);
            smtpClient.EnableSsl = true;
            smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Overdue property notification sent successfully to {Count} admin(s) for {PropertyCount} overdue property(ies)", 
                mailMessage.To.Count, overdueProperties.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send overdue property notification to admins");
            return false;
        }
    }
}

