using Microsoft.Extensions.Logging;
using UstazAI.Application.Ai;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Common.Services;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;

namespace UstazAI.Application.Diagnostics;

public sealed class DiagnosticsService(IAiReasoningService ai, DecisionLedgerWriter ledger, ICurrentUser user, ILogger<DiagnosticsService> logger)
{
    public async Task<Domain.Entities.Diagnostics> GenerateAsync(StudentProfile profile, CancellationToken ct)
    {
        DiagnosticsAiOutput output;
        try
        {
            output = await ai.GenerateDiagnosticsAsync(new DiagnosticsAiInput(
                profile.FullName, profile.Grade, profile.Gpa, profile.Interests, profile.TargetCountries,
                profile.BudgetBand, profile.Constraints, user.UiLocale ?? profile.PreferredLanguage), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI diagnostics failed for profile {ProfileId}; using deterministic fallback", profile.Id);
            output = FallbackDiagnostics(profile, user.UiLocale ?? profile.PreferredLanguage);
        }

        var diagnostics = new Domain.Entities.Diagnostics
        {
            StudentProfileId = profile.Id,
            ProfileVersion = profile.Version,
            Strengths = output.Strengths,
            ConstraintsFound = output.ConstraintsFound,
            InferredGoal = output.InferredGoal,
            ConfidenceScore = output.ConfidenceScore,
            IsAiGenerated = !output.FallbackUsed,
            FallbackUsed = output.FallbackUsed
        };

        await ledger.AppendAsync(profile.Id, AiDecisionType.Diagnostics, output, ct);
        return diagnostics;
    }

    private static DiagnosticsAiOutput FallbackDiagnostics(StudentProfile profile, Locale l)
    {
        var strengths = new List<string>();
        if (profile.Gpa is { } gpa && gpa >= 3.5m) strengths.Add(L10n.Tr(l, "Strong overall GPA", "Высокий общий средний балл (GPA)", "Жоғары жалпы орташа балл (GPA)"));
        if (profile.ExamScores.Count > 0) strengths.Add(L10n.Tr(l, "Has documented exam scores on record", "Есть подтверждённые результаты экзаменов", "Емтихан нәтижелері расталған"));
        if (profile.Interests.Count > 0)
        {
            var list = string.Join(", ", profile.Interests);
            strengths.Add(L10n.Tr(l, $"Clear interest area: {list}", $"Чёткая область интересов: {list}", $"Нақты қызығушылық аймағы: {list}"));
        }
        if (strengths.Count == 0) strengths.Add(L10n.Tr(l, "Profile recorded — add exam scores and GPA for a sharper diagnostic",
            "Профиль сохранён — добавьте баллы экзаменов и GPA для более точной диагностики",
            "Профиль сақталды — нақтырақ диагностика үшін емтихан балдары мен GPA қосыңыз"));

        var constraints = new List<string>();
        if (profile.BudgetBand == BudgetBand.Low && profile.FundingTrackPreference != FundingTrackPreference.GrantOnly)
            constraints.Add(L10n.Tr(l, "Limited budget band may narrow international options without scholarships",
                "Ограниченный бюджет может сузить выбор зарубежных вариантов без стипендий",
                "Шектеулі бюджет стипендиясыз шетелдік нұсқаларды азайтуы мүмкін"));
        if (profile.TimelineMonthsToApplication < 6)
            constraints.Add(L10n.Tr(l, "Short timeline before application deadlines", "Мало времени до дедлайнов подачи документов", "Құжат тапсыру мерзіміне дейін уақыт аз"));
        if (constraints.Count == 0) constraints.Add(L10n.Tr(l, "No major constraints detected from structured profile data",
            "По данным профиля серьёзных ограничений не обнаружено", "Профиль деректері бойынша елеулі шектеулер анықталмады"));

        var field = profile.Interests.Count > 0 ? profile.Interests[0] : L10n.Tr(l, "a field matching your academic strengths", "направление, подходящее вашим сильным сторонам", "академиялық күшті жақтарыңызға сай бағыт");
        var country = profile.TargetCountries.Count > 0 ? profile.TargetCountries[0] : L10n.Tr(l, "a country you specify", "выбранная вами страна", "өзіңіз таңдаған ел");
        var goal = L10n.Tr(l, $"Pursue {field} in {country}", $"Развиваться в направлении «{field}»: {country}", $"«{field}» бағытында дамыту: {country}");

        return new DiagnosticsAiOutput(strengths, constraints, goal, 0.5, true);
    }
}
