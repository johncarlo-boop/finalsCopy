using PropertyInventory.Services;

namespace PropertyInventory.Middleware;

public class RequirePasswordChangeMiddleware
{
    private readonly RequestDelegate _next;

    public RequirePasswordChangeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
    {
        // Skip middleware for login, logout, change password, and static files
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.StartsWith("/account/login") ||
            path.StartsWith("/account/mobilelogin") ||
            path.StartsWith("/account/logout") ||
            path.StartsWith("/account/changepassword") ||
            path.StartsWith("/account/forgotpassword") ||
            path.StartsWith("/account/register") ||
            path.StartsWith("/_") ||
            path.StartsWith("/css") ||
            path.StartsWith("/js") ||
            path.StartsWith("/images") ||
            path.StartsWith("/profile-pictures") ||
            path.StartsWith("/lib") ||
            !context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        // Create a scope to resolve scoped services
        using (var scope = serviceProvider.CreateScope())
        {
            var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
            
            // Check if password change is required
            if (authService.RequiresPasswordChange())
            {
                context.Response.Redirect("/Account/ChangePassword");
                return;
            }
        }

        await _next(context);
    }
}

