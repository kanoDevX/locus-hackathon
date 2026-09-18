using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Dtos;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;

namespace UstazAI.Application.Programs;

public sealed record SearchProgramsQuery(
    string? Query, string? Country, string? FieldOfStudy, DegreeLevel? DegreeLevel,
    decimal? MaxTuitionUsd, bool? ScholarshipOnly) : IRequest<List<ProgramSummaryDto>>;

/// <summary>
/// Catalog browse/search — lets a student explore the seeded programs directly (school-list
/// building), not only through the AI-ranked recommendation flow. The unfiltered catalog is
/// cached in memory for 5 minutes (§2 Caching: "IMemoryCache for hot recommendation results") —
/// the seed catalog is effectively static during a demo, so this trades a small staleness window
/// for far fewer DB round trips on the hottest read path in the API.
/// </summary>
public sealed class SearchProgramsHandler(IAppDbContext db, IMemoryCache cache)
    : IRequestHandler<SearchProgramsQuery, List<ProgramSummaryDto>>
{
    private const string CacheKey = "catalog:all-programs";

    public async Task<List<ProgramSummaryDto>> Handle(SearchProgramsQuery query, CancellationToken ct)
    {
        var all = await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await db.ProgramOfferings.Include(p => p.University).AsNoTracking().ToListAsync(ct);
        }) ?? [];

        IEnumerable<ProgramOffering> results = all;

        if (!string.IsNullOrWhiteSpace(query.Query))
            results = results.Where(p =>
                p.Name.Contains(query.Query, StringComparison.OrdinalIgnoreCase) ||
                p.FieldOfStudy.Contains(query.Query, StringComparison.OrdinalIgnoreCase) ||
                p.University.Name.Contains(query.Query, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(query.Country))
            results = results.Where(p => p.University.Country.Contains(query.Country, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(query.FieldOfStudy))
            results = results.Where(p => p.FieldOfStudy.Contains(query.FieldOfStudy, StringComparison.OrdinalIgnoreCase));

        if (query.DegreeLevel is { } level)
            results = results.Where(p => p.DegreeLevel == level);

        if (query.MaxTuitionUsd is { } maxTuition)
            results = results.Where(p => p.TuitionPerYearUsd <= maxTuition);

        if (query.ScholarshipOnly == true)
            results = results.Where(p => p.ScholarshipAvailable);

        return [.. results.OrderBy(p => p.Name).Select(p => p.ToDto())];
    }
}
