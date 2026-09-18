using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UstazAI.Application.Ai;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Common.Services;
using UstazAI.Application.Dtos;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;
using UstazAI.Domain.Services;
using DiagnosticsEntity = UstazAI.Domain.Entities.Diagnostics;

namespace UstazAI.Application.Chat;

public sealed record SendChatMessageCommand(Guid ProfileId, Guid UserId, string Message) : IRequest<ChatMessageDto>;

public sealed class SendChatMessageValidator : AbstractValidator<SendChatMessageCommand>
{
    public SendChatMessageValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2_000);
    }
}

public sealed class SendChatMessageHandler(IAppDbContext db, IAiReasoningService ai, DecisionLedgerWriter ledger, ICurrentUser user, ILogger<SendChatMessageHandler> logger)
    : IRequestHandler<SendChatMessageCommand, ChatMessageDto>
{
    private const int HistoryTurns = 10;

    public async Task<ChatMessageDto> Handle(SendChatMessageCommand cmd, CancellationToken ct)
    {
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(p => p.Id == cmd.ProfileId && p.UserId == cmd.UserId, ct)
            ?? throw new KeyNotFoundException($"Profile {cmd.ProfileId} not found");

        var history = await db.ChatMessages
            .Where(m => m.StudentProfileId == profile.Id)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(HistoryTurns)
            .ToListAsync(ct);
        history.Reverse();

        var topEligibility = (await db.EligibilityResults
            .Include(r => r.Program)
            .Where(r => r.StudentProfileId == profile.Id && r.ProfileVersion == profile.Version)
            .ToListAsync(ct))
            .OrderByDescending(r => r.GrantCompetitiveness.PointEstimate)
            .Take(5)
            .ToList();

        var diagnostics = await db.Diagnostics
            .Where(d => d.StudentProfileId == profile.Id)
            .OrderByDescending(d => d.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        var scopedContext = BuildScopedContext(profile, topEligibility, diagnostics);

        db.ChatMessages.Add(new ChatMessage { StudentProfileId = profile.Id, Role = ChatRole.User, Content = cmd.Message, IsAiGenerated = false });

        string reply;
        bool fallbackUsed;
        try
        {
            var output = await ai.ChatAsync(new ChatAiInput(
                cmd.Message, scopedContext,
                [.. history.Select(m => new ChatTurn(m.Role.ToString(), m.Content))],
                user.UiLocale ?? profile.PreferredLanguage), ct);
            reply = output.Reply;
            fallbackUsed = output.FallbackUsed;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI chat failed for profile {ProfileId}; using deterministic fallback", profile.Id);
            reply = BuildFallbackReply(topEligibility, diagnostics, user.UiLocale ?? profile.PreferredLanguage);
            fallbackUsed = true;
        }

        reply = GuardrailRules.Sanitize(reply);

        var assistantMessage = new ChatMessage
        {
            StudentProfileId = profile.Id,
            Role = ChatRole.Assistant,
            Content = reply,
            IsAiGenerated = !fallbackUsed,
            FallbackUsed = fallbackUsed
        };
        db.ChatMessages.Add(assistantMessage);

        await ledger.AppendAsync(profile.Id, AiDecisionType.Chat, new { UserMessageLength = cmd.Message.Length, fallbackUsed }, ct);
        await db.SaveChangesAsync(ct);

        return assistantMessage.ToDto();
    }

    private static string BuildScopedContext(StudentProfile profile, List<EligibilityResult> topEligibility, DiagnosticsEntity? diagnostics) =>
        JsonSerializer.Serialize(new
        {
            profile.EducationStage,
            profile.FundingTrackPreference,
            Eligibility = topEligibility.Select(r => new
            {
                Program = r.Program.Name,
                r.MeetsStateThreshold,
                r.MeetsUniversityThreshold,
                GrantCompetitivenessPercent = r.GrantCompetitiveness.PointEstimate,
                r.IsDocumentOnlyVerdict,
                r.Notes
            }),
            Diagnostics = diagnostics is null ? null : new
            {
                diagnostics.Strengths,
                diagnostics.ConstraintsFound,
                diagnostics.InferredGoal
            }
        });

    private static string BuildFallbackReply(List<EligibilityResult> topEligibility, DiagnosticsEntity? diagnostics, Locale l)
    {
        if (topEligibility.Count == 0)
        {
            return L10n.Tr(l,
                "I can't reach the AI assistant right now, and there are no eligibility results on file yet — submit your exam intake and run the eligibility calculation first, and I'll be able to talk through your options in detail.",
                "Сейчас ИИ-помощник недоступен, а результатов проверки шансов пока нет — сначала заполните данные об экзаменах и запустите расчёт шансов, тогда я смогу подробно разобрать ваши варианты.",
                "Қазір ЖИ-көмекші қолжетімсіз, ал мүмкіндік нәтижелері әлі жоқ — алдымен емтихан деректерін толтырып, мүмкіндікті есептеңіз, сонда нұсқаларыңызды толық талдай аламын.");
        }

        var best = topEligibility[0];
        var name = best.Program.Name;
        var pct = best.GrantCompetitiveness.PointEstimate.ToString("0");
        var summary = best.IsDocumentOnlyVerdict
            ? L10n.Tr(l,
                $"{name} is a document-only admission path for you — no exam score is required, admission goes through the university's own commission.",
                $"{name} — для вас путь поступления только по документам: балл экзамена не нужен, приём идёт через комиссию университета.",
                $"{name} — сіз үшін тек құжат бойынша түсу жолы: емтихан балы қажет емес, қабылдау университет комиссиясы арқылы өтеді.")
            : L10n.Tr(l,
                $"for {name}, your grant competitiveness is estimated around {pct}% ({best.GrantCompetitiveness.Basis}).",
                $"по программе {name} ваша конкурентоспособность на грант оценивается примерно в {pct}%.",
                $"{name} бағдарламасы бойынша грантқа бәсекеге қабілеттілігіңіз шамамен {pct}% деп бағаланады.");

        var strengths = "";
        if (diagnostics is { Strengths.Count: > 0 })
        {
            var list = string.Join(", ", diagnostics.Strengths.Take(2));
            strengths = L10n.Tr(l, $" Your diagnostics also flagged: {list}.", $" В диагностике также отмечено: {list}.", $" Диагностикада да атап өтілген: {list}.");
        }

        return L10n.Tr(l,
                $"I can't reach the AI assistant right now, so here's what's already on file: {summary}{strengths} Try asking again in a moment for a fuller, AI-narrated answer.",
                $"Сейчас ИИ-помощник недоступен, поэтому вот что уже есть в данных: {summary}{strengths} Попробуйте спросить снова чуть позже — получите более полный ответ.",
                $"Қазір ЖИ-көмекші қолжетімсіз, сондықтан бар деректер бойынша: {summary}{strengths} Сәлден кейін қайта сұраңыз — толығырақ жауап аласыз.");
    }
}
