using System.Security.Claims;

namespace TaskManagement.API.Extensions;

public static class ClaimsExtensions
{
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("UserId claim missing."));

    public static int GetTenantId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue("tenantId")
            ?? throw new InvalidOperationException("TenantId claim missing."));

    public static bool IsAdmin(this ClaimsPrincipal user) =>
        user.IsInRole("Admin");
}
