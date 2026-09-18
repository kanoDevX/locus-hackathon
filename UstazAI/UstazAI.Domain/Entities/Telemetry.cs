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

/// <summary>
/// Append-only, hash-chained log of every scoring/AI decision (see §10.9). Each row hashes
/// its own payload together with the previous row's hash, so the chain can be walked to prove
/// no entry was altered after the fact — the concrete backing for "every recommendation this
/// product has ever given is reproducible and auditable".
/// </summary>
public sealed class AiDecisionLog : AuditableEntity<long>
{
    public Guid StudentProfileId { get; set; }
    public AiDecisionType DecisionType { get; set; }
    public string PayloadJson { get; set; } = default!;
    public string PreviousHash { get; set; } = default!;
    public string Hash { get; set; } = default!;
}
