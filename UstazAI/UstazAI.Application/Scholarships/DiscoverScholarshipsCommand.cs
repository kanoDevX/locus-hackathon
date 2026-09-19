using FluentValidation;
using MediatR;
using UstazAI.Application.Ai;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Domain.Services;

namespace UstazAI.Application.Scholarships;

public sealed record DiscoverScholarshipsCommand(string UniversityName, string Country) : IRequest<DiscoverScholarshipsResult>;

public sealed record DiscoveredScholarshipDto(string Name, string Coverage, string Eligibility, string Deadline);

public sealed record ScholarshipSourceDto(string Title, string Url);

public sealed record DiscoverScholarshipsResult(
    string UniversityName, string Country, List<DiscoveredScholarshipDto> Scholarships, List<ScholarshipSourceDto> Sources, DateTime RetrievedAtUtc)
    : ISanitizableAiResponse
{
    public List<DiscoveredScholarshipDto> Scholarships { get; set; } = Scholarships;

    public void SanitizeAiText() =>
        Scholarships = [.. Scholarships.Select(s => new DiscoveredScholarshipDto(
            GuardrailRules.Sanitize(s.Name), GuardrailRules.Sanitize(s.Coverage), GuardrailRules.Sanitize(s.Eligibility), GuardrailRules.Sanitize(s.Deadline)))];
}

public sealed class DiscoverScholarshipsValidator : AbstractValidator<DiscoverScholarshipsCommand>
{
    public DiscoverScholarshipsValidator()
    {
        RuleFor(x => x.UniversityName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(80);
    }
}

public sealed class DiscoverScholarshipsHandler(IAiReasoningService ai, ICurrentUser user)
    : IRequestHandler<DiscoverScholarshipsCommand, DiscoverScholarshipsResult>
{
    public async Task<DiscoverScholarshipsResult> Handle(DiscoverScholarshipsCommand cmd, CancellationToken ct)
    {
        ScholarshipDiscoveryOutput r;
        try
        {
            r = await ai.DiscoverScholarshipsAsync(new ScholarshipDiscoveryInput(cmd.UniversityName.Trim(), cmd.Country.Trim(), null), ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            throw new InvalidOperationException(T(
                "Google Search quota for the AI key is used up for today — try again tomorrow.",
                "Дневной лимит поиска в интернете для ключа ИИ исчерпан — попробуйте завтра.",
                "ЖИ кілті үшін интернеттен іздеудің күндік лимиті таусылды — ертең қайталап көріңіз."));
        }
        catch (Exception)
        {
            throw new InvalidOperationException(T(
                "Live web research is unavailable right now.",
                "Поиск в интернете сейчас недоступен.",
                "Интернеттен іздеу қазір қолжетімсіз."));
        }

        if (r.Sources.Count == 0)
            throw new InvalidOperationException(T(
                "No citable sources were found, so nothing is shown.",
                "Источников со ссылками не найдено, поэтому ничего не показываем.",
                "Сілтемелі дереккөздер табылмады, сондықтан ештеңе көрсетілмейді."));

        var items = r.Scholarships.Select(s => new DiscoveredScholarshipDto(s.Name, s.Coverage, s.Eligibility, s.Deadline)).ToList();
        var sources = r.Sources.Select(s => new ScholarshipSourceDto(s.Title, s.Url)).ToList();
        return new DiscoverScholarshipsResult(cmd.UniversityName.Trim(), cmd.Country.Trim(), items, sources, DateTime.UtcNow);
    }

    private string T(string en, string ru, string kk) => L10n.Tr(user.UiLocale ?? Domain.Enums.Locale.En, en, ru, kk);
}
