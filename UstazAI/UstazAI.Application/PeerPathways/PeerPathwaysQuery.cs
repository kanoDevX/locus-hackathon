using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Domain.Enums;

namespace UstazAI.Application.PeerPathways;

public sealed record PeerPathwaysQuery(Guid ProfileId, Guid UserId, int ProgramId) : IRequest<PeerPathwaysResultDto>;

public sealed record PeerArchetypeGroupDto(
    string ArchetypeLabel, AdmitOutcome Outcome, int Count, int TypicalTimelineMonths, string? CommonBlocker);

public sealed record PeerPathwaysResultDto(int SampleSize, List<PeerArchetypeGroupDto> Groups, string Disclaimer);

public sealed class PeerPathwaysHandler(IAppDbContext db) : IRequestHandler<PeerPathwaysQuery, PeerPathwaysResultDto>
{
    public async Task<PeerPathwaysResultDto> Handle(PeerPathwaysQuery query, CancellationToken ct)
    {
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(p => p.Id == query.ProfileId && p.UserId == query.UserId, ct)
            ?? throw new KeyNotFoundException($"Profile {query.ProfileId} not found");

        var archetypes = await db.AdmitArchetypes.Where(a => a.ProgramId == query.ProgramId).ToListAsync(ct);

        var matched = archetypes.Where(a =>
        {
            var gpaOverlaps = profile.Gpa is null ||
                ((double)profile.Gpa >= (double)a.GpaMin - 0.3 && (double)profile.Gpa <= (double)a.GpaMax + 0.3);
            var budgetMatches = a.BudgetBand == profile.BudgetBand;
            return gpaOverlaps && budgetMatches;
        }).ToList();

        var groups = matched
            .GroupBy(a => (a.ArchetypeLabel, a.Outcome))
            .Select(g => new PeerArchetypeGroupDto(
                g.Key.ArchetypeLabel, g.Key.Outcome, g.Sum(a => a.Weight),
                (int)Math.Round(g.Average(a => a.TimelineMonths)),
                g.Select(a => a.CommonBlocker).FirstOrDefault(b => b is not null)))
            .OrderByDescending(g => g.Count)
            .ToList();

        var disclaimer = matched.Count == 0
            ? "No seeded peer archetypes matched this profile's GPA range and budget band for this program — this is illustrative demo data, not a guarantee of any outcome."
            : "Distribution over seeded, anonymized archetype profiles similar to yours. Illustrative demo data, not a guarantee of any outcome.";

        return new PeerPathwaysResultDto(matched.Sum(a => a.Weight), groups, disclaimer);
    }
}
