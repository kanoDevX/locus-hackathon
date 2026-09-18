using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Domain.Enums;

namespace UstazAI.Application.Auth;

public sealed record AccountDto(Guid UserId, string Email, string DisplayName, UserRole Role);

public sealed record UpdateAccountCommand(Guid UserId, string DisplayName, string Email) : IRequest<AccountDto>;

public sealed class UpdateAccountValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public sealed class UpdateAccountHandler(IAppDbContext db) : IRequestHandler<UpdateAccountCommand, AccountDto>
{
    public async Task<AccountDto> Handle(UpdateAccountCommand cmd, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == cmd.UserId, ct)
            ?? throw new KeyNotFoundException("Account not found.");

        if (!string.Equals(user.Email, cmd.Email, StringComparison.OrdinalIgnoreCase)
            && await db.Users.AnyAsync(u => u.Email == cmd.Email && u.Id != cmd.UserId, ct))
        {
            throw new InvalidOperationException("That email is already in use by another account.");
        }

        user.DisplayName = cmd.DisplayName;
        user.Email = cmd.Email;
        await db.SaveChangesAsync(ct);

        return new AccountDto(user.Id, user.Email, user.DisplayName, user.Role);
    }
}

public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).MinimumLength(8).WithMessage("Password must be at least 8 characters.");
    }
}

public sealed class ChangePasswordHandler(IAppDbContext db, IPasswordHasher hasher) : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand cmd, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == cmd.UserId, ct)
            ?? throw new KeyNotFoundException("Account not found.");

        if (!hasher.Verify(cmd.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.PasswordHash = hasher.Hash(cmd.NewPassword);

        var tokens = await db.RefreshTokens.Where(t => t.UserId == cmd.UserId && !t.Revoked).ToListAsync(ct);
        foreach (var token in tokens) token.Revoked = true;

        await db.SaveChangesAsync(ct);
    }
}
