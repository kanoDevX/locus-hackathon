using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;

namespace UstazAI.Application.Auth;

public sealed record AuthResultDto(
    Guid UserId, string Email, string DisplayName, UserRole Role,
    string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAtUtc);

public sealed record RegisterCommand(string Email, string Password, string DisplayName) : IRequest<AuthResultDto>;

public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(8).WithMessage("Password must be at least 8 characters.");
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
    }
}

public sealed class RegisterHandler(IAppDbContext db, IPasswordHasher hasher, IJwtTokenService jwt)
    : IRequestHandler<RegisterCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Email == cmd.Email, ct))
            throw new InvalidOperationException("Email is already registered.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = cmd.Email,
            PasswordHash = hasher.Hash(cmd.Password),
            DisplayName = cmd.DisplayName,
            Role = UserRole.Student
        };
        db.Users.Add(user);

        var tokens = jwt.IssueTokens(user.Id, user.Email, user.Role);
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(), UserId = user.Id, TokenHash = jwt.HashRefreshToken(tokens.RefreshToken),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
        });

        await db.SaveChangesAsync(ct);
        return new AuthResultDto(user.Id, user.Email, user.DisplayName, user.Role,
            tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiresAtUtc);
    }
}

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResultDto>;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginHandler(IAppDbContext db, IPasswordHasher hasher, IJwtTokenService jwt)
    : IRequestHandler<LoginCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == cmd.Email, ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!hasher.Verify(cmd.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var tokens = jwt.IssueTokens(user.Id, user.Email, user.Role);
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(), UserId = user.Id, TokenHash = jwt.HashRefreshToken(tokens.RefreshToken),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
        });

        await db.SaveChangesAsync(ct);
        return new AuthResultDto(user.Id, user.Email, user.DisplayName, user.Role,
            tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiresAtUtc);
    }
}

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResultDto>;

public sealed class RefreshTokenHandler(IAppDbContext db, IJwtTokenService jwt)
    : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var hash = jwt.HashRefreshToken(cmd.RefreshToken);
        var token = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash && !t.Revoked && t.ExpiresAtUtc > DateTime.UtcNow, ct)
            ?? throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == token.UserId, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        token.Revoked = true;

        var tokens = jwt.IssueTokens(user.Id, user.Email, user.Role);
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(), UserId = user.Id, TokenHash = jwt.HashRefreshToken(tokens.RefreshToken),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
        });

        await db.SaveChangesAsync(ct);
        return new AuthResultDto(user.Id, user.Email, user.DisplayName, user.Role,
            tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiresAtUtc);
    }
}

/// <summary>Revokes one refresh token (single-device logout). Access tokens are short-lived by
/// design (see JwtOptions.AccessTokenMinutes) and are not separately blacklisted — revoking the
/// refresh token stops the session from renewing once the access token expires.</summary>
public sealed record LogoutCommand(string RefreshToken) : IRequest;

public sealed class LogoutHandler(IAppDbContext db, IJwtTokenService jwt) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand cmd, CancellationToken ct)
    {
        var hash = jwt.HashRefreshToken(cmd.RefreshToken);
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is not null && !token.Revoked)
        {
            token.Revoked = true;
            await db.SaveChangesAsync(ct);
        }
    }
}

/// <summary>Revokes every active refresh token for the calling user — "log out everywhere",
/// useful after a suspected compromise or a shared demo device.</summary>
public sealed record LogoutAllCommand(Guid UserId) : IRequest;

public sealed class LogoutAllHandler(IAppDbContext db) : IRequestHandler<LogoutAllCommand>
{
    public async Task Handle(LogoutAllCommand cmd, CancellationToken ct)
    {
        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == cmd.UserId && !t.Revoked)
            .ToListAsync(ct);

        foreach (var token in tokens) token.Revoked = true;
        await db.SaveChangesAsync(ct);
    }
}
