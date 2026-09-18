using UstazAI.Domain.Common;
using UstazAI.Domain.Enums;

namespace UstazAI.Domain.Entities;

public sealed class User : AuditableEntity<Guid>
{
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public UserRole Role { get; set; } = UserRole.Student;

    public Guid? OrganizationId { get; set; }

    public List<RefreshToken> RefreshTokens { get; set; } = [];
}

public sealed class RefreshToken : AuditableEntity<Guid>
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
    public bool Revoked { get; set; }
}
