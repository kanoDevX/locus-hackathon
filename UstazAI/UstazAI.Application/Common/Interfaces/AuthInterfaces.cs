using UstazAI.Domain.Enums;

namespace UstazAI.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    UserRole? Role { get; }

    Locale? UiLocale { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public sealed record IssuedTokens(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAtUtc);

public interface IJwtTokenService
{
    IssuedTokens IssueTokens(Guid userId, string email, UserRole role);
    string HashRefreshToken(string refreshToken);
}
