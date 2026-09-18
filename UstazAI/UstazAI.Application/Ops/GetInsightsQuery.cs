using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common.Interfaces;

namespace UstazAI.Application.Ops;

public sealed record GetInsightsQuery : IRequest<InsightsDto>;

public sealed record ModuleBreakdownDto(string Module, int Calls, double SuccessRatePercent, double FallbackRatePercent);

public sealed record InsightsDto(
    int TotalAiCalls, double SuccessRatePercent, double FallbackRatePercent,
    double AverageLatencyMs, double P95LatencyMs, long TotalInputTokens, long TotalOutputTokens,
    List<ModuleBreakdownDto> ByModule);

public sealed class GetInsightsHandler(IAppDbContext db) : IRequestHandler<GetInsightsQuery, InsightsDto>
{
    public async Task<InsightsDto> Handle(GetInsightsQuery query, CancellationToken ct)
    {
        var logs = await db.AiUsageLogs.OrderByDescending(l => l.CreatedAtUtc).Take(2000).ToListAsync(ct);
        if (logs.Count == 0)
            return new InsightsDto(0, 0, 0, 0, 0, 0, 0, []);

        var latencies = logs.Select(l => (double)l.LatencyMs).OrderBy(x => x).ToList();
        var p95Index = (int)Math.Ceiling(latencies.Count * 0.95) - 1;

        var byModule = logs.GroupBy(l => l.Module)
            .Select(g => new ModuleBreakdownDto(
                g.Key, g.Count(),
                Math.Round(g.Count(x => x.Success) * 100.0 / g.Count(), 1),
                Math.Round(g.Count(x => x.FallbackUsed) * 100.0 / g.Count(), 1)))
            .ToList();

        return new InsightsDto(
            logs.Count,
            Math.Round(logs.Count(l => l.Success) * 100.0 / logs.Count, 1),
            Math.Round(logs.Count(l => l.FallbackUsed) * 100.0 / logs.Count, 1),
            Math.Round(latencies.Average(), 1),
            Math.Round(latencies[Math.Clamp(p95Index, 0, latencies.Count - 1)], 1),
            logs.Sum(l => (long)l.InputTokens),
            logs.Sum(l => (long)l.OutputTokens),
            byModule);
    }
}
