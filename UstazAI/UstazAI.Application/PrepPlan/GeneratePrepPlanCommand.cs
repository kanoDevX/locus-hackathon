using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Common.Services;
using UstazAI.Application.Dtos;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;
using UstazAI.Domain.Services;

namespace UstazAI.Application.PrepPlan;

public sealed record GeneratePrepPlanCommand(Guid ProfileId, Guid UserId, int ProgramId) : IRequest<List<RoadmapTaskDto>>;

public sealed class GeneratePrepPlanHandler(IAppDbContext db, DecisionLedgerWriter ledger)
    : IRequestHandler<GeneratePrepPlanCommand, List<RoadmapTaskDto>>
{
    private const int MaxPrepTasks = 3;

    public async Task<List<RoadmapTaskDto>> Handle(GeneratePrepPlanCommand cmd, CancellationToken ct)
    {
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(p => p.Id == cmd.ProfileId && p.UserId == cmd.UserId, ct)
            ?? throw new KeyNotFoundException($"Profile {cmd.ProfileId} not found");

        var program = await db.ProgramOfferings
            .Include(p => p.AdmissionThresholds)
            .FirstOrDefaultAsync(p => p.Id == cmd.ProgramId, ct)
            ?? throw new KeyNotFoundException($"Program {cmd.ProgramId} not found");

        var examRecord = await db.ExamRecords
            .Where(r => r.StudentProfileId == profile.Id)
            .OrderByDescending(r => r.CreatedAtUtc)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No exam record on file — submit the exam intake first (POST /exam-intake).");

        var threshold = program.AdmissionThresholds.FirstOrDefault(t => t.Track == examRecord.Track);
        var gaps = GapAnalysisEngine.AnalyzeGaps(examRecord, threshold).Take(MaxPrepTasks).ToList();

        var staleIds = await db.RoadmapTasks
            .Where(t => t.StudentProfileId == profile.Id && t.ProgramId == cmd.ProgramId
                && t.Category == RoadmapTaskCategory.SubjectPrep && t.Status == RoadmapTaskStatus.NotStarted)
            .Select(t => t.Id)
            .ToListAsync(ct);
        await RoadmapTaskCleanup.RemoveTasksAsync(db, staleIds, ct);
        await db.SaveChangesAsync(ct);

        var deadline = program.ApplicationDeadline;
        var created = new List<RoadmapTask>();
        foreach (var gap in gaps)
        {
            var task = new RoadmapTask
            {
                StudentProfileId = profile.Id,
                ProgramId = cmd.ProgramId,
                Title = $"Strengthen {gap.SubjectName}",
                Description = $"You scored {gap.CurrentScore}/{gap.MaxScore} on {gap.SubjectName} in your latest exam record — " +
                    $"{gap.HeadroomPoints} points of unclaimed headroom, ranked #{gap.PriorityRank} among your gaps for {program.Name}.",
                Category = RoadmapTaskCategory.SubjectPrep,
                DueDate = deadline.AddDays(-60),
                UrgencyScore = Math.Clamp(100 - (gap.PriorityRank - 1) * 15, 40, 100),
                ImpactScore = Math.Clamp((int)(gap.HeadroomPoints * 3), 40, 100),
                IsAiGenerated = false,
                Subject = gap.SubjectName,
                Resources = ResourceLinkCatalog.ForSubject(gap.SubjectName)
            };
            created.Add(task);
            db.RoadmapTasks.Add(task);
        }

        await ledger.AppendAsync(profile.Id, AiDecisionType.PrepPlan, new
        {
            cmd.ProgramId,
            Gaps = gaps.Select(g => new { g.SubjectName, g.HeadroomPoints, g.PriorityRank })
        }, ct);
        await db.SaveChangesAsync(ct);

        return [.. created.Select(t => t.ToDto())];
    }
}

public sealed record GetPrepPlanQuery(Guid ProfileId, Guid UserId, int ProgramId) : IRequest<List<RoadmapTaskDto>>;

public sealed class GetPrepPlanHandler(IAppDbContext db) : IRequestHandler<GetPrepPlanQuery, List<RoadmapTaskDto>>
{
    public async Task<List<RoadmapTaskDto>> Handle(GetPrepPlanQuery query, CancellationToken ct)
    {
        var ownsProfile = await db.StudentProfiles.AnyAsync(p => p.Id == query.ProfileId && p.UserId == query.UserId, ct);
        if (!ownsProfile) throw new KeyNotFoundException($"Profile {query.ProfileId} not found");

        var tasks = await db.RoadmapTasks
            .Where(t => t.StudentProfileId == query.ProfileId && t.ProgramId == query.ProgramId
                && t.Category == RoadmapTaskCategory.SubjectPrep)
            .OrderByDescending(t => t.UrgencyScore)
            .ToListAsync(ct);

        return [.. tasks.Select(t => t.ToDto())];
    }
}
