using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Common.Services;
using UstazAI.Application.Dtos;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;
using UstazAI.Domain.Services;

namespace UstazAI.Application.ExamIntake;

public sealed record CalculateEligibilityCommand(Guid ProfileId, Guid UserId) : IRequest<List<EligibilityResultDto>>;

public sealed class CalculateEligibilityHandler(IAppDbContext db, DecisionLedgerWriter ledger)
    : IRequestHandler<CalculateEligibilityCommand, List<EligibilityResultDto>>
{
    public async Task<List<EligibilityResultDto>> Handle(CalculateEligibilityCommand cmd, CancellationToken ct)
    {
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(p => p.Id == cmd.ProfileId && p.UserId == cmd.UserId, ct)
            ?? throw new KeyNotFoundException($"Profile {cmd.ProfileId} not found");

        var examRecord = await db.ExamRecords
            .Where(r => r.StudentProfileId == profile.Id)
            .OrderByDescending(r => r.CreatedAtUtc)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No exam record on file — submit the exam intake first (POST /exam-intake).");

        var domesticPrograms = examRecord.Track == AdmissionExamTrack.NotTakingEnt
            ? []
            : await db.ProgramOfferings
                .Include(p => p.University)
                .Include(p => p.AdmissionThresholds)
                .Where(p => p.University.Country == "Kazakhstan")
                .ToListAsync(ct);

        var staleAtThisVersion = await db.EligibilityResults
            .Where(r => r.StudentProfileId == profile.Id && r.ProfileVersion == profile.Version)
            .ToListAsync(ct);
        if (staleAtThisVersion.Count > 0) db.EligibilityResults.RemoveRange(staleAtThisVersion);

        var results = new List<EligibilityResult>();
        foreach (var program in domesticPrograms)
        {
            var threshold = program.AdmissionThresholds.FirstOrDefault(t => t.Track == examRecord.Track);
            var verdict = GrantEligibilityEngine.Evaluate(examRecord, threshold);

            var result = new EligibilityResult
            {
                StudentProfileId = profile.Id,
                ProfileVersion = profile.Version,
                ProgramId = program.Id,
                Program = program,
                Track = examRecord.Track,
                MeetsStateThreshold = verdict.MeetsStateThreshold,
                MeetsUniversityThreshold = verdict.MeetsUniversityThreshold,
                GrantCompetitiveness = verdict.GrantCompetitiveness,
                PaidTrackEligible = verdict.PaidTrackEligible,
                IsDocumentOnlyVerdict = verdict.IsDocumentOnlyVerdict,
                Notes = verdict.Notes,
                Provenance = threshold?.Provenance ?? Domain.ValueObjects.DataProvenance.Demo("No threshold data seeded for this program/track")
            };
            db.EligibilityResults.Add(result);
            results.Add(result);
        }

        await ledger.AppendAsync(profile.Id, AiDecisionType.EligibilityCheck, new
        {
            examRecord.Track,
            examRecord.TotalScore,
            Verdicts = results.Select(r => new { r.ProgramId, r.MeetsStateThreshold, r.MeetsUniversityThreshold, r.GrantCompetitiveness.PointEstimate })
        }, ct);

        await db.SaveChangesAsync(ct);

        return [.. results.OrderByDescending(r => r.GrantCompetitiveness.PointEstimate).Select(r => r.ToDto())];
    }
}
