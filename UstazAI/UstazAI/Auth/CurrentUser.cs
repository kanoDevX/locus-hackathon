using System.Security.Claims;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Domain.Enums;

namespace UstazAI.Auth;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var sub = Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal?.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public UserRole? Role
    {
        get
        {
            var role = Principal?.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(role, out var parsed) ? parsed : null;
        }
    }

    public Locale? UiLocale =>
        accessor.HttpContext?.Request.Headers["X-Locale"].ToString().Trim().ToLowerInvariant() switch
        {
            "ru" => Locale.Ru,
            "kk" => Locale.Kk,
            "en" => Locale.En,
            _ => null
        };
}
