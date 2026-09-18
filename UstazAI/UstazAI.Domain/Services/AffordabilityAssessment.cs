using UstazAI.Domain.Enums;

namespace UstazAI.Domain.Services;

public sealed record AffordabilityAssessment(AffordabilityTier Tier, decimal EffectiveCostUsd, decimal BudgetCeilingUsd);
