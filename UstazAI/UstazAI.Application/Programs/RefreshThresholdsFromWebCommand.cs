using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Ai;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Application.Programs;

public sealed record RefreshThresholdsFromWebCommand(int ProgramId, AdmissionExamTrack Track) : IRequest<RefreshThresholdsFromWebResult>;

public sealed record RefreshThresholdsFromWebResult(List<string> UpdatedFields, List<string> SourceNames);

/// <summary>
/// Replaces seeded demo ENT/grant thresholds for one program+track with live, source-cited ones
/// (Gemini + Google Search). Same honesty rules as RefreshProgramFromWebHandler: no cited source
/// means no update; every score is bounds-checked (0-140) and the set must be self-consistent
/// (state ≤ university threshold, min ≤ median ≤ max) or that group is skipped; anything not
/// confirmed keeps its old value. The row is only marked verified for what was actually found.
/// Callers should re-run POST /exam-intake/calculate afterwards — verdicts are snapshots.
/// </summary>
public sealed class RefreshThresholdsFromWebHandler(IAppDbContext db, IAiReasoningService ai, ICurrentUser user)
    : IRequestHandler<RefreshThresholdsFromWebCommand, RefreshThresholdsFromWebResult>
{
    private static bool Score(decimal? v) => v is >= 0 and <= 140;

    public async Task<RefreshThresholdsFromWebResult> Handle(RefreshThresholdsFromWebCommand cmd, CancellationToken ct)
    {
        if (cmd.Track is AdmissionExamTrack.ContinuingSpecialtyPaid or AdmissionExamTrack.NotTakingEnt)
            throw new InvalidOperationException(T("This admission path has no exam thresholds to research.", "У этого пути поступления нет экзаменационных порогов для поиска.", "Бұл түсу жолында іздейтін емтихан шектері жоқ."));

        var program = await db.ProgramOfferings.Include(p => p.University).Include(p => p.AdmissionThresholds)
            .FirstOrDefaultAsync(p => p.Id == cmd.ProgramId, ct)
            ?? throw new KeyNotFoundException($"Program {cmd.ProgramId} not found");

        ThresholdResearchOutput r;
        try
        {
            r = await ai.ResearchThresholdsAsync(new ThresholdResearchInput(program.Name, program.University.Name, cmd.Track.ToString()), ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            throw new InvalidOperationException(T("Google Search quota for the AI key is used up for today — try again tomorrow. Nothing was changed.", "Дневной лимит поиска в интернете для ключа ИИ исчерпан — попробуйте завтра. Данные не изменены.", "ЖИ кілті үшін интернеттен іздеудің күндік лимиті таусылды — ертең қайталап көріңіз. Деректер өзгертілмеді."));
        }
        catch (Exception)
        {
            throw new InvalidOperationException(T("Live web research is unavailable right now — the existing data was left unchanged.", "Поиск в интернете сейчас недоступен — данные не изменены.", "Интернеттен іздеу қазір қолжетімсіз — деректер өзгертілмеді."));
        }

        if (r.Sources.Count == 0)
            throw new InvalidOperationException(T("No citable sources were found for these thresholds, so nothing was changed.", "Для этих порогов не найдено источников со ссылками — данные не изменены.", "Бұл шектер үшін сілтемелі дереккөздер табылмады — деректер өзгертілмеді."));

        var threshold = program.AdmissionThresholds.FirstOrDefault(t => t.Track == cmd.Track);
        var isNew = threshold is null;
        threshold ??= new AdmissionThreshold { ProgramId = program.Id, Track = cmd.Track };

        var updated = new List<string>();
        var state = Score(r.StateThreshold) ? r.StateThreshold : null;
        var uni = Score(r.UniversityThreshold) ? r.UniversityThreshold : null;
        if (state is not null && uni is not null && uni < state) uni = null;

        if (state is { } s) { threshold.StateThreshold = s; updated.Add("stateThreshold"); }
        if (uni is { } u)
        {
            // a program's own minimum can only be raised above the state floor, never lowered below it
            threshold.UniversityInternalThreshold = Math.Max(u, threshold.StateThreshold);
            updated.Add("universityThreshold");
        }
        else if (isNew && state is not null)
        {
            threshold.UniversityInternalThreshold = threshold.StateThreshold;
        }

        if (Score(r.CutoffMin) && Score(r.CutoffMax) && Score(r.CutoffMedian)
            && r.CutoffMin <= r.CutoffMedian && r.CutoffMedian <= r.CutoffMax)
        {
            threshold.HistoricalCutoffMin = r.CutoffMin!.Value;
            threshold.HistoricalCutoffMax = r.CutoffMax!.Value;
            threshold.HistoricalCutoffMedian = r.CutoffMedian!.Value;
            threshold.HistoricalCutoffSampleSize = Math.Clamp(r.CutoffYearsCount ?? 1, 1, 20);
            updated.Add("grantCutoffs");
        }

        if (updated.Count == 0)
            throw new InvalidOperationException(T("The web sources didn't confirm any scores for this program, so nothing was changed.", "Источники в интернете не подтвердили баллы для этой программы — данные не изменены.", "Интернет дереккөздері бұл бағдарлама үшін балдарды растамады — деректер өзгертілмеді."));

        var names = r.Sources.Select(x => string.IsNullOrWhiteSpace(x.Title) ? "web" : x.Title).Distinct().ToList();
        threshold.Provenance = DataProvenance.Verified(
            "Researched live via Gemini + Google Search. Sources: " + string.Join(", ", names) + " — always confirm on the official university / ministry site.",
            DateTime.UtcNow);

        if (isNew) db.AdmissionThresholds.Add(threshold);
        await db.SaveChangesAsync(ct);

        return new RefreshThresholdsFromWebResult(updated, names);
    }

    private string T(string en, string ru, string kk) => L10n.Tr(user.UiLocale ?? Domain.Enums.Locale.En, en, ru, kk);
}
