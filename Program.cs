using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using PropertyInventory.Hubs;
using PropertyInventory.Middleware;
using PropertyInventory.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Data Protection to use persistent storage
var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("PropertyInventory");

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews().AddViewComponentsAsServices();
builder.Services.AddScoped<FirebaseService>();
builder.Services.AddScoped<QrCodeService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddHttpContextAccessor();

// Add background service for overdue property notifications
builder.Services.AddHostedService<OverduePropertyNotificationService>();

// Add Authentication (Cookie-based)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Add SignalR for real-time updates
builder.Services.AddSignalR();

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    // In production (Render), HTTPS is handled by the load balancer
    // Only redirect to HTTPS if we're not in a containerized environment
    app.UseHttpsRedirection();
}
else
{
    // In development, enable HTTPS redirection for camera access
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Add middleware to require password change if needed
app.UseMiddleware<RequirePasswordChangeMiddleware>();

app.MapRazorPages();
app.MapHub<PropertyHub>("/propertyHub");

// Configure to listen on the PORT environment variable (provided by Render)
// Only use HTTP - Render handles HTTPS termination at the load balancer
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
var url = $"http://0.0.0.0:{port}";
app.Urls.Clear();
app.Urls.Add(url);

app.Run();
