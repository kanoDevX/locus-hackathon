using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Ai;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Dtos;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Application.Programs;

public sealed record RefreshProgramFromWebCommand(int ProgramId) : IRequest<RefreshProgramFromWebResult>;

public sealed record RefreshProgramFromWebResult(
    ProgramSummaryDto Program, DataProvenanceDto Provenance, List<string> UpdatedFields, List<string> SourceUrls);

public sealed class RefreshProgramFromWebHandler(IAppDbContext db, IAiReasoningService ai, ICurrentUser user)
    : IRequestHandler<RefreshProgramFromWebCommand, RefreshProgramFromWebResult>
{
    public async Task<RefreshProgramFromWebResult> Handle(RefreshProgramFromWebCommand cmd, CancellationToken ct)
    {
        var program = await db.ProgramOfferings.Include(p => p.University).FirstOrDefaultAsync(p => p.Id == cmd.ProgramId, ct)
            ?? throw new KeyNotFoundException($"Program {cmd.ProgramId} not found");

        ProgramResearchOutput research;
        try
        {
            research = await ai.ResearchProgramAsync(new ProgramResearchInput(
                program.Name, program.University.Name, program.University.Country, program.DegreeLevel.ToString()), ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            throw new InvalidOperationException(T("Google Search quota for the AI key is used up for today — try again tomorrow. Nothing was changed.", "Дневной лимит поиска в интернете для ключа ИИ исчерпан — попробуйте завтра. Данные не изменены.", "ЖИ кілті үшін интернеттен іздеудің күндік лимиті таусылды — ертең қайталап көріңіз. Деректер өзгертілмеді."));
        }
        catch (Exception)
        {
            throw new InvalidOperationException(T("Live web research is unavailable right now — the existing data was left unchanged.", "Поиск в интернете сейчас недоступен — данные не изменены.", "Интернеттен іздеу қазір қолжетімсіз — деректер өзгертілмеді."));
        }

        if (research.Sources.Count == 0)
            throw new InvalidOperationException(T("No citable sources were found for this program, so nothing was changed.", "Для этой программы не найдено источников со ссылками — данные не изменены.", "Бұл бағдарлама үшін сілтемелі дереккөздер табылмады — деректер өзгертілмеді."));

        var updated = new List<string>();
        if (research.TuitionPerYearUsd is >= 0 and <= 150_000) { program.TuitionPerYearUsd = research.TuitionPerYearUsd.Value; updated.Add("tuition"); }
        if (research.LivingCostPerYearUsd is >= 0 and <= 60_000) { program.LivingCostPerYearUsd = research.LivingCostPerYearUsd.Value; updated.Add("livingCost"); }
        if (research.ApplicationDeadline is { } d && d >= DateOnly.FromDateTime(DateTime.UtcNow)) { program.ApplicationDeadline = d; updated.Add("deadline"); }
        if (research.ScholarshipAvailable is { } sa)
        {
            program.ScholarshipAvailable = sa;
            if (!sa) program.ScholarshipCoveragePercent = 0;
            updated.Add("scholarshipAvailable");
        }
        if (research.ScholarshipAvailable != false && research.ScholarshipCoveragePercent is >= 0 and <= 100)
        {
            program.ScholarshipCoveragePercent = research.ScholarshipCoveragePercent.Value;
            updated.Add("scholarshipCoverage");
        }

        if (updated.Count == 0)
            throw new InvalidOperationException(T("The web sources didn't confirm any figures for this program, so nothing was changed.", "Источники в интернете не подтвердили цифры для этой программы — данные не изменены.", "Интернет дереккөздері бұл бағдарлама үшін сандарды растамады — деректер өзгертілмеді."));

        var urls = research.Sources.Select(s => s.Url).ToList();
        program.Provenance = DataProvenance.Verified(
            "Researched live via Gemini + Google Search. Sources: " + string.Join(", ", research.Sources.Select(x => string.IsNullOrWhiteSpace(x.Title) ? "web" : x.Title).Distinct()) + " — always confirm on the official university site.",
            DateTime.UtcNow);

        await db.SaveChangesAsync(ct);

        return new RefreshProgramFromWebResult(program.ToDto(), program.Provenance.ToDto(), updated, urls);
    }

    private string T(string en, string ru, string kk) => L10n.Tr(user.UiLocale ?? Domain.Enums.Locale.En, en, ru, kk);
}
