using UstazAI.Application.Common.Interfaces;
using UstazAI.Domain.Services;

namespace UstazAI.Application.Dtos;

/// <summary>`YouTubeSearchUrl` is a real, functional `youtube.com/results?search_query=...` link
/// built from the AI-suggested search phrase — never a specific video (see StudyGuideStep's own
/// doc comment on the Domain side for why: no video-metadata API is wired up, so a specific
/// title/id/thumbnail would have to be invented, and this product never fabricates a fact).</summary>
public sealed record StudyGuideStepDto(
    int StepNumber, string Title, string Description, int EstimatedMinutes, string VideoSearchQuery, string YouTubeSearchUrl);

/// <summary>Mutable (not a record) — see RecommendationDto's doc comment for why: each step's
/// Title/Description is Gemini-generated free text that needs GuardrailBehavior's SanitizeAiText
/// last-resort net to run on it. StudyGuideStepDto itself stays an immutable record; sanitizing
/// replaces Steps with `with`-derived copies rather than mutating each step in place.</summary>
public sealed class StudyGuideDto : ISanitizableAiResponse
{
    public int Id { get; set; }
    public int RoadmapTaskId { get; set; }
    public required string Subject { get; set; }
    public required List<StudyGuideStepDto> Steps { get; set; }
    public bool IsAiGenerated { get; set; }
    public bool FallbackUsed { get; set; }
    public required DataProvenanceDto Provenance { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public void SanitizeAiText()
    {
        Steps = [.. Steps.Select(s => s with { Title = GuardrailRules.Sanitize(s.Title), Description = GuardrailRules.Sanitize(s.Description) })];
    }
}
