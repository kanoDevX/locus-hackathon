namespace UstazAI.Domain.Common;

public abstract class AuditableEntity<TId>
{
    public TId Id { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
