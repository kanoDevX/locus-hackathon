using UstazAI.Domain.Common;
using UstazAI.Domain.Enums;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Domain.Entities;

public sealed class Diagnostics : AuditableEntity<int>
{
    public Guid StudentProfileId { get; set; }
    public int ProfileVersion { get; set; }

    public List<string> Strengths { get; set; } = [];
    public List<string> ConstraintsFound { get; set; } = [];
    public string InferredGoal { get; set; } = default!;
    public double ConfidenceScore { get; set; }

    public bool IsAiGenerated { get; set; }
    public bool FallbackUsed { get; set; }
}

public sealed class Recommendation : AuditableEntity<int>
{
    public Guid StudentProfileId { get; set; }
    public int ProfileVersion { get; set; }
    public int ProgramId { get; set; }
    public ProgramOffering Program { get; set; } = default!;

    public int RankPosition { get; set; }
    public double OverallScore { get; set; }

    public double AcademicFitScore { get; set; }
    public double FinancialFitScore { get; set; }
    public double CareerFitScore { get; set; }
    public double TimelineFitScore { get; set; }

    public string AcademicFitExplanation { get; set; } = default!;
    public string FinancialFitExplanation { get; set; } = default!;
    public string CareerFitExplanation { get; set; } = default!;
    public string TimelineFitExplanation { get; set; } = default!;
    public string NarrativeSummary { get; set; } = default!;

    public UncertaintyEstimate AdmissionProbability { get; set; } = new();
    public DataProvenance Provenance { get; set; } = DataProvenance.Demo("UstazAI hybrid scorer");

    public bool IsAiNarrated { get; set; }
    public bool FallbackUsed { get; set; }

    public AffordabilityTier AffordabilityTier { get; set; }
    public decimal AffordabilityEffectiveCostUsd { get; set; }
    public decimal AffordabilityBudgetCeilingUsd { get; set; }
}

public sealed class ChatMessage : AuditableEntity<int>
{
    public Guid StudentProfileId { get; set; }
    public ChatRole Role { get; set; }
    public string Content { get; set; } = default!;

    public bool IsAiGenerated { get; set; }
    public bool FallbackUsed { get; set; }
}
