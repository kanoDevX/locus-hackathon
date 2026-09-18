using UstazAI.Domain.Enums;

namespace UstazAI.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    UserRole? Role { get; }

    /// <summary>The language the UI is currently displayed in (X-Locale request header). Wins over
    /// the profile's stored PreferredLanguage for AI-generated text: a student browsing in Russian
    /// with a profile saved as English otherwise got English explanations inside a Russian page.</summary>
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
