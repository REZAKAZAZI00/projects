using System.Security.Claims;

namespace Licensing.Api.Extensions;

public static class HttpContextExtensions
{
    public static string? GetClientIp(this HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString();

    public static string GetActor(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Name) ?? "anonymous";
}
