using FluentAssertions;
using UstazAI.Application.Common.Behaviors;
using UstazAI.Application.Dtos;
using UstazAI.Domain.Services;
using MediatR;

namespace UstazAI.Tests;

/// <summary>
/// Locks in a real bug found by code audit: RecommendationDto, DiagnosticsDto and StudyGuideDto
/// carried Gemini-narrated free text but never implemented ISanitizableAiResponse, so
/// GuardrailBehavior's last-resort forbidden-language net (§5.3) silently never ran on them —
/// the single highest-risk surface (admission-probability narration) had no guardrail at all.
/// Also covers GetLatestRecommendationsQuery's response shape specifically: it returns a bare
/// `List&lt;RecommendationDto&gt;`, which can never itself implement the marker interface (you
/// cannot add an interface to a BCL collection type), so GuardrailBehavior needed to recurse into
/// enumerable responses too, not just check the top-level response's own type.
/// </summary>
public class GuardrailBehaviorTests
{
    private const string Forbidden = "This program guarantees admission for top students.";

    private static ProgramSummaryDto Program() => new(
        1, "Computer Science", "Test University", "Kazakhstan", "Astana", "CS", Domain.Enums.DegreeLevel.Bachelor,
        "English", 5000, 3000, new DateOnly(2027, 1, 1), true, 50, 20, 30000);

    private static RecommendationDto Recommendation() => new()
    {
        RecommendationId = 1,
        Program = Program(),
        RankPosition = 1,
        AcademicFitExplanation = Forbidden,
        FinancialFitExplanation = Forbidden,
        CareerFitExplanation = Forbidden,
        TimelineFitExplanation = Forbidden,
        NarrativeSummary = Forbidden,
        AdmissionProbability = new UncertaintyEstimateDto(80, 60, 90, 5, "test"),
        Provenance = new DataProvenanceDto("test", true, DateTime.UtcNow)
    };

    [Fact]
    public void RecommendationDto_SanitizeAiText_RewritesEveryNarratedField()
    {
        var dto = Recommendation();

        dto.SanitizeAiText();

        GuardrailRules.ContainsForbiddenLanguage(dto.AcademicFitExplanation).Should().BeFalse();
        GuardrailRules.ContainsForbiddenLanguage(dto.FinancialFitExplanation).Should().BeFalse();
        GuardrailRules.ContainsForbiddenLanguage(dto.CareerFitExplanation).Should().BeFalse();
        GuardrailRules.ContainsForbiddenLanguage(dto.TimelineFitExplanation).Should().BeFalse();
        GuardrailRules.ContainsForbiddenLanguage(dto.NarrativeSummary).Should().BeFalse();
    }

    [Fact]
    public void DiagnosticsDto_SanitizeAiText_RewritesStrengthsConstraintsAndGoal()
    {
        var dto = new DiagnosticsDto
        {
            Strengths = [Forbidden],
            ConstraintsFound = [Forbidden],
            InferredGoal = Forbidden
        };

        dto.SanitizeAiText();

        dto.Strengths.Should().OnlyContain(s => !GuardrailRules.ContainsForbiddenLanguage(s));
        dto.ConstraintsFound.Should().OnlyContain(s => !GuardrailRules.ContainsForbiddenLanguage(s));
        GuardrailRules.ContainsForbiddenLanguage(dto.InferredGoal).Should().BeFalse();
    }

    [Fact]
    public void StudyGuideDto_SanitizeAiText_RewritesEveryStepTitleAndDescription()
    {
        var dto = new StudyGuideDto
        {
            Subject = "Mathematics",
            Steps = [new StudyGuideStepDto(1, Forbidden, Forbidden, 60, "algebra basics", "https://youtube.com/results?search_query=algebra")],
            Provenance = new DataProvenanceDto("test", true, DateTime.UtcNow)
        };

        dto.SanitizeAiText();

        GuardrailRules.ContainsForbiddenLanguage(dto.Steps[0].Title).Should().BeFalse();
        GuardrailRules.ContainsForbiddenLanguage(dto.Steps[0].Description).Should().BeFalse();
    }

    [Fact]
    public async Task GuardrailBehavior_SanitizesEveryItemInABareListResponse()
    {
        // GetLatestRecommendationsQuery : IRequest<List<RecommendationDto>> — the response type
        // IS the list, so only GuardrailBehavior's IEnumerable recursion (not the top-level
        // ISanitizableAiResponse check alone) can reach each RecommendationDto inside it.
        var behavior = new GuardrailBehavior<FakeListRequest, List<RecommendationDto>>();
        var recommendations = new List<RecommendationDto> { Recommendation(), Recommendation() };

        var result = await behavior.Handle(new FakeListRequest(), (_) => Task.FromResult(recommendations), CancellationToken.None);

        result.Should().OnlyContain(r => !GuardrailRules.ContainsForbiddenLanguage(r.NarrativeSummary));
    }

    [Fact]
    public async Task GuardrailBehavior_SanitizesAWrappingResultThatImplementsTheInterfaceItself()
    {
        var behavior = new GuardrailBehavior<FakeWrapperRequest, UstazAI.Application.Recommendations.GenerateRecommendationsResult>();
        var wrapped = new UstazAI.Application.Recommendations.GenerateRecommendationsResult(
            [Recommendation()], new RecommendationDeltaDto(false, null, [], [], []), new AffordabilitySummaryDto(1, 0, 0));

        var result = await behavior.Handle(new FakeWrapperRequest(), (_) => Task.FromResult(wrapped), CancellationToken.None);

        GuardrailRules.ContainsForbiddenLanguage(result.Recommendations[0].NarrativeSummary).Should().BeFalse();
    }

    public sealed record FakeListRequest : IRequest<List<RecommendationDto>>;
    public sealed record FakeWrapperRequest : IRequest<UstazAI.Application.Recommendations.GenerateRecommendationsResult>;
}
