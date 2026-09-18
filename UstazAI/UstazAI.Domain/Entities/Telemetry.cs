using UstazAI.Domain.Common;
using UstazAI.Domain.Enums;

namespace UstazAI.Domain.Entities;

public sealed class AiUsageLog : AuditableEntity<long>
{
    public Guid? StudentProfileId { get; set; }
    public string Module { get; set; } = default!;
    public string Model { get; set; } = default!;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public long LatencyMs { get; set; }
    public bool Success { get; set; }
    public bool FallbackUsed { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class AiDecisionLog : AuditableEntity<long>
{
    public Guid StudentProfileId { get; set; }
    public AiDecisionType DecisionType { get; set; }
    public string PayloadJson { get; set; } = default!;
    public string PreviousHash { get; set; } = default!;
    public string Hash { get; set; } = default!;
}
