using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Dtos;

namespace UstazAI.Application.Recommendations;

public sealed record GenerateRecommendationsCommand(Guid ProfileId, Guid UserId) : IRequest<GenerateRecommendationsResult>;

/// <summary>A record can still implement ISanitizableAiResponse: SanitizeAiText doesn't need to
/// reassign this type's own (init-only) properties, only call each already-mutable
/// RecommendationDto's own SanitizeAiText — this wrapper's response type is
/// GenerateRecommendationsResult itself (not a bare List), so GuardrailBehavior's generic
/// IEnumerable recursion doesn't reach into Recommendations on its own; this makes it explicit.</summary>
public sealed record GenerateRecommendationsResult(
    List<RecommendationDto> Recommendations, RecommendationDeltaDto Delta, AffordabilitySummaryDto Affordability) : ISanitizableAiResponse
{
    public void SanitizeAiText()
    {
        foreach (var r in Recommendations) r.SanitizeAiText();
    }
}

/// <summary>
/// Always recomputes against the current profile version. Comparing this batch's program ids to
/// the most recent PRIOR batch is exactly how the "changing one survey answer visibly and
/// traceably changes recommendations" hard requirement is demonstrated: call this endpoint,
/// change the profile, call it again, and Delta shows precisely what moved.
/// </summary>
public sealed class GenerateRecommendationsHandler(IAppDbContext db, RecommendationEngine engine)
    : IRequestHandler<GenerateRecommendationsCommand, GenerateRecommendationsResult>
{
    public async Task<GenerateRecommendationsResult> Handle(GenerateRecommendationsCommand cmd, CancellationToken ct)
    {
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(p => p.Id == cmd.ProfileId && p.UserId == cmd.UserId, ct)
            ?? throw new KeyNotFoundException($"Profile {cmd.ProfileId} not found");

        var previousBatch = await db.Recommendations
            .Where(r => r.StudentProfileId == profile.Id && r.ProfileVersion < profile.Version)
            .OrderByDescending(r => r.ProfileVersion)
            .ToListAsync(ct);
        var previousVersion = previousBatch.Count > 0 ? previousBatch[0].ProfileVersion : (int?)null;
        var previousProgramRanks = previousBatch
            .Where(r => r.ProfileVersion == previousVersion)
            .GroupBy(r => r.ProgramId)
            .ToDictionary(g => g.Key, g => g.Min(r => r.RankPosition));

        var recommendations = await engine.GenerateAsync(profile, ct);
        await db.SaveChangesAsync(ct);

        var currentProgramIds = recommendations.Select(r => r.ProgramId).ToList();
        var added = currentProgramIds.Except(previousProgramRanks.Keys).ToList();
        var removed = previousProgramRanks.Keys.Except(currentProgramIds).ToList();
        var rankChanged = recommendations
            .Where(r => previousProgramRanks.TryGetValue(r.ProgramId, out var prevRank) && prevRank != r.RankPosition)
            .Select(r => r.ProgramId)
            .ToList();

        var delta = new RecommendationDeltaDto(previousVersion is not null, previousVersion, added, removed, rankChanged);

        var affordability = new AffordabilitySummaryDto(
            recommendations.Count(r => r.AffordabilityTier == Domain.Enums.AffordabilityTier.Affordable),
            recommendations.Count(r => r.AffordabilityTier == Domain.Enums.AffordabilityTier.Stretch),
            recommendations.Count(r => r.AffordabilityTier == Domain.Enums.AffordabilityTier.OverBudget));

        return new GenerateRecommendationsResult([.. recommendations.Select(r => r.ToDto())], delta, affordability);
    }
}
