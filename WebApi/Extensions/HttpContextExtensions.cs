using Hospital.Domain.Entities;

namespace Hospital.WebApi.Extensions;

public static class HttpContextExtensions
{
    public static Guid? GetCurrentUserId(this HttpContext context)
    {
        if (context == null) return null;

        if (context.Items.TryGetValue( "CurrentUserId", out var idObj) && idObj is Guid id) return id;
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var sid = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(sid, out var parsed)) return parsed;
        }

        return null;
    }

    public static User? GetCurrentUser(this HttpContext context)
    {
        if (context == null) return null;
        if (context.Items.TryGetValue( "CurrentUser", out var userObj) && userObj is User user) return user;
        return null;
    }
}