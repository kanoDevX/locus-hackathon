using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UstazAI.Application.Ai;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Common.Services;
using UstazAI.Application.Dtos;
using UstazAI.Domain.Enums;

namespace UstazAI.Application.Essays;

public sealed record ReviewEssayCommand(Guid ProfileId, Guid UserId, string EssayText, string? Prompt)
    : IRequest<EssayReviewDto>;

public sealed class ReviewEssayValidator : AbstractValidator<ReviewEssayCommand>
{
    public ReviewEssayValidator()
    {
        RuleFor(x => x.EssayText).NotEmpty().MinimumLength(50).MaximumLength(20_000);
    }
}

/// <summary>
/// Essay help (case brief baseline feature): feedback only, never a rewritten draft — admissions
/// offices actively screen for AI-generated essays, so both the Gemini prompt and the
/// deterministic fallback are structurally incapable of returning a replacement essay.
/// </summary>
public sealed class ReviewEssayHandler(IAppDbContext db, IAiReasoningService ai, DecisionLedgerWriter ledger, ICurrentUser user, ILogger<ReviewEssayHandler> logger)
    : IRequestHandler<ReviewEssayCommand, EssayReviewDto>
{
    private static readonly string[] ClichePhrases =
    [
        "since the beginning of time", "in today's society", "little did i know",
        "as the saying goes", "it is what it is", "dictionary defines"
    ];

    public async Task<EssayReviewDto> Handle(ReviewEssayCommand cmd, CancellationToken ct)
    {
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(p => p.Id == cmd.ProfileId && p.UserId == cmd.UserId, ct)
            ?? throw new KeyNotFoundException($"Profile {cmd.ProfileId} not found");

        EssayReviewAiOutput output;
        try
        {
            output = await ai.ReviewEssayAsync(new EssayReviewAiInput(cmd.EssayText, cmd.Prompt, user.UiLocale ?? profile.PreferredLanguage), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI essay review failed for profile {ProfileId}; using deterministic fallback", profile.Id);
            output = FallbackReview(cmd.EssayText, user.UiLocale ?? profile.PreferredLanguage);
        }

        await ledger.AppendAsync(profile.Id, AiDecisionType.EssayReview, new { EssayLength = cmd.EssayText.Length, output.FallbackUsed }, ct);
        await db.SaveChangesAsync(ct);

        return new EssayReviewDto
        {
            Strengths = output.Strengths,
            SuggestedImprovements = output.SuggestedImprovements,
            ClarityFeedback = output.ClarityFeedback,
            StructureFeedback = output.StructureFeedback,
            IsAiGenerated = !output.FallbackUsed,
            FallbackUsed = output.FallbackUsed
        };
    }

    private static EssayReviewAiOutput FallbackReview(string essayText, Locale l)
    {
        var words = essayText.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);
        var wordCount = words.Length;
        var sentences = essayText.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries);
        var avgWordsPerSentence = sentences.Length > 0 ? (double)wordCount / sentences.Length : 0;
        var lower = essayText.ToLowerInvariant();

        var strengths = new List<string>();
        var improvements = new List<string>();

        if (wordCount is >= 250 and <= 650)
            strengths.Add(L10n.Tr(l, "Length is within a typical strong range for an admission essay.", "Объём укладывается в типичный хороший диапазон для вступительного эссе.", "Көлемі түсу эссесі үшін жақсы диапазонға сәйкес келеді."));
        else if (wordCount < 250)
            improvements.Add(L10n.Tr(l, $"At {wordCount} words, the essay is on the short side — consider developing one moment in more specific, concrete detail.", $"В эссе {wordCount} слов — это коротковато: раскройте один эпизод подробнее и конкретнее.", $"Эсседе {wordCount} сөз — қысқа: бір сәтті нақтырақ әрі толығырақ ашыңыз."));
        else
            improvements.Add(L10n.Tr(l, $"At {wordCount} words, the essay is quite long — look for sentences to tighten toward the most vivid details.", $"В эссе {wordCount} слов — длинновато: сократите предложения, оставив самые яркие детали.", $"Эсседе {wordCount} сөз — ұзын: ең жарқын бөлшектерді қалдырып, сөйлемдерді қысқартыңыз."));

        if (essayText.Contains('I', StringComparison.Ordinal) || lower.Contains(" i "))
            strengths.Add(L10n.Tr(l, "Written in a personal, first-person voice.", "Написано личным голосом, от первого лица.", "Жеке тұлғалық, бірінші жақтан жазылған."));

        foreach (var phrase in ClichePhrases)
        {
            if (lower.Contains(phrase))
                improvements.Add($"Consider replacing the generic phrase \"{phrase}\" with something more specific to your own experience.");
        }

        if (improvements.Count == 0)
            improvements.Add(L10n.Tr(l, "No obvious issues found by the automatic checklist — consider asking a teacher or mentor to read it for tone and voice.", "Автоматическая проверка явных проблем не нашла — попросите учителя или наставника оценить тон и голос эссе.", "Автоматты тексеру айқын мәселе тапқан жоқ — мұғалімнен немесе тәлімгерден эссенің тоны мен стилін бағалауды сұраңыз."));

        var avg = avgWordsPerSentence.ToString("0.0");
        var clarity = avgWordsPerSentence > 28
            ? L10n.Tr(l, $"Average sentence length is ~{avg} words — some sentences may be long enough to hurt clarity; consider breaking a few up (simplified automatic check — AI review unavailable).", $"Средняя длина предложения ~{avg} слов — часть предложений слишком длинная, разбейте некоторые (упрощённая автопроверка — ИИ-разбор недоступен).", $"Сөйлемнің орташа ұзындығы ~{avg} сөз — кейбірі тым ұзын, бөліп жазыңыз (жеңілдетілген автотексеру — ЖИ талдауы қолжетімсіз).")
            : L10n.Tr(l, $"Average sentence length is ~{avg} words, which reads reasonably (simplified automatic check — AI review unavailable).", $"Средняя длина предложения ~{avg} слов — читается нормально (упрощённая автопроверка — ИИ-разбор недоступен).", $"Сөйлемнің орташа ұзындығы ~{avg} сөз — оқуға ыңғайлы (жеңілдетілген автотексеру — ЖИ талдауы қолжетімсіз).");

        var structure = L10n.Tr(l, "Automatic structure feedback unavailable without AI — check for yourself whether the opening line hooks the reader with a specific moment rather than a general statement, and whether the ending connects back to it.", "Без ИИ автоматический разбор структуры недоступен — проверьте сами, цепляет ли первая фраза конкретным эпизодом, а не общими словами, и возвращается ли концовка к ней.", "ЖИ-сіз құрылымды автоматты талдау қолжетімсіз — алғашқы сөйлем жалпы сөзбен емес, нақты сәтпен қызықтыра ма және соңы соған оралады ма, өзіңіз тексеріңіз.");

        return new EssayReviewAiOutput(strengths, improvements, clarity, structure, true);
    }
}
