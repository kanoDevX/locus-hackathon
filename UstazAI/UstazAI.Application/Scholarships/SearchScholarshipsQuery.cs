using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Dtos;

namespace UstazAI.Application.Scholarships;

public sealed record SearchScholarshipsQuery(
    string? Country, decimal? MinCoveragePercent, string? Query) : IRequest<List<ScholarshipSearchResultDto>>;

public sealed class SearchScholarshipsHandler(IAppDbContext db)
    : IRequestHandler<SearchScholarshipsQuery, List<ScholarshipSearchResultDto>>
{
    public async Task<List<ScholarshipSearchResultDto>> Handle(SearchScholarshipsQuery query, CancellationToken ct)
    {
        var scholarships = await db.Scholarships.ToListAsync(ct);
        var programIds = scholarships.Where(s => s.ProgramId.HasValue).Select(s => s.ProgramId!.Value).Distinct().ToList();
        var programs = await db.ProgramOfferings.Include(p => p.University)
            .Where(p => programIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var results = scholarships.Select(s =>
        {
            var program = s.ProgramId.HasValue && programs.TryGetValue(s.ProgramId.Value, out var p) ? p : null;
            return (Scholarship: s, Program: program);
        });

        if (query.MinCoveragePercent is { } minCoverage)
            results = results.Where(r => r.Scholarship.CoveragePercent >= minCoverage);

        if (!string.IsNullOrWhiteSpace(query.Country))
            results = results.Where(r => r.Program is null ||
                r.Program.University.Country.Contains(query.Country, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(query.Query))
            results = results.Where(r =>
                r.Scholarship.Name.Contains(query.Query, StringComparison.OrdinalIgnoreCase) ||
                (r.Program is not null && r.Program.Name.Contains(query.Query, StringComparison.OrdinalIgnoreCase)));

        return [.. results
            .OrderByDescending(r => r.Scholarship.CoveragePercent)
            .Select(r => new ScholarshipSearchResultDto(
                r.Scholarship.Id, r.Scholarship.Name, r.Scholarship.CoveragePercent, r.Scholarship.EligibilityCriteria,
                r.Scholarship.DeadlineDate, r.Program?.ToDto(), r.Scholarship.Provenance.ToDto()))];
    }
}
