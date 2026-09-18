using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Common.Services;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;
using UstazAI.Domain.Services;

namespace UstazAI.Application.WhatIf;

public sealed record WhatIfQuery(Guid ProfileId, Guid UserId, int ProgramId) : IRequest<List<WhatIfOptionDto>>;

public sealed record WhatIfOptionDto(
    string Variable, string Description, double BaselineOverallScore, double SimulatedOverallScore,
    double OverallScoreDelta, double BaselineAdmissionProbability, double SimulatedAdmissionProbability,
    double AdmissionProbabilityDelta);

/// <summary>
/// Counterfactual simulator (§10.4): perturbs one realistic variable at a time against the
/// deterministic hybrid scorer and ranks which single change would move the needle most. Pure
/// computation, no AI call — cheap, fast, and the deterministic model is the same one the
/// Recommendations stage already trusts.
/// </summary>
public sealed class WhatIfHandler(IAppDbContext db, DecisionLedgerWriter ledger) : IRequestHandler<WhatIfQuery, List<WhatIfOptionDto>>
{
    public async Task<List<WhatIfOptionDto>> Handle(WhatIfQuery query, CancellationToken ct)
    {
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(p => p.Id == query.ProfileId && p.UserId == query.UserId, ct)
            ?? throw new KeyNotFoundException($"Profile {query.ProfileId} not found");

        var program = await db.ProgramOfferings
            .Include(p => p.University)
            .Include(p => p.AdmitArchetypes)
            .FirstOrDefaultAsync(p => p.Id == query.ProgramId, ct)
            ?? throw new KeyNotFoundException($"Program {query.ProgramId} not found");

        var baseline = HybridScoringEngine.Score(profile, program, program.AdmitArchetypes);

        var options = new List<WhatIfOptionDto>();

        void AddOption(string variable, string description, StudentProfile perturbed)
        {
            var simulated = HybridScoringEngine.Score(perturbed, program, program.AdmitArchetypes);
            options.Add(new WhatIfOptionDto(
                variable, description,
                baseline.OverallScore, simulated.OverallScore, Math.Round(simulated.OverallScore - baseline.OverallScore, 1),
                baseline.AdmissionProbability.PointEstimate, simulated.AdmissionProbability.PointEstimate,
                Math.Round(simulated.AdmissionProbability.PointEstimate - baseline.AdmissionProbability.PointEstimate, 1)));
        }

        if (profile.Gpa is { } gpa)
        {
            var p = profile.Clone();
            p.Gpa = Math.Min(4.0m, gpa + 0.3m);
            AddOption("Gpa", $"Raise GPA from {gpa:0.00} to {p.Gpa:0.00}", p);
        }

        if (profile.ExamScores.Count > 0)
        {
            var p = profile.Clone();
            p.ExamScores = [.. p.ExamScores.Select(e => new Domain.ValueObjects.ExamScore
            {
                ExamType = e.ExamType, DateTaken = e.DateTaken, MaxScore = e.MaxScore,
                Score = Math.Min(e.MaxScore, e.Score * 1.1m)
            })];
            AddOption("ExamScores", "Raise every exam score by 10% (capped at max score)", p);
        }

        if (profile.FundingTrackPreference != FundingTrackPreference.GrantOnly && profile.BudgetBand != BudgetBand.VeryHigh)
        {
            var p = profile.Clone();
            p.BudgetBand = profile.BudgetBand + 1;
            AddOption("BudgetBand", $"Move budget band up from {profile.BudgetBand} to {p.BudgetBand}", p);
        }

        {
            var p = profile.Clone();
            p.TimelineMonthsToApplication += 3;
            AddOption("Timeline", $"Extend planning timeline from {profile.TimelineMonthsToApplication} to {p.TimelineMonthsToApplication} months", p);
        }

        var ranked = options.OrderByDescending(o => o.AdmissionProbabilityDelta).ToList();

        await ledger.AppendAsync(profile.Id, AiDecisionType.WhatIf, new { query.ProgramId, Options = ranked }, ct);
        await db.SaveChangesAsync(ct);

        return ranked;
    }
}
