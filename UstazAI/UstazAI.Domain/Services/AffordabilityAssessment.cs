using UstazAI.Domain.Enums;

namespace UstazAI.Domain.Services;

/// <summary>
/// Pure output of HybridScoringEngine.EvaluateAffordability (§10.6) for one (profile, program)
/// pair — the deterministic tier and cost figures behind the "Affordability & Fairness Audit"
/// disclosure, computed once at recommendation time and persisted on the Recommendation entity
/// itself so it survives if the profile's budget band changes later (same snapshot-at-version
/// discipline every other scored field on that entity already follows).
/// </summary>
public sealed record AffordabilityAssessment(AffordabilityTier Tier, decimal EffectiveCostUsd, decimal BudgetCeilingUsd);
