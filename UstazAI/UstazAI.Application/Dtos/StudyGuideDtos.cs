using UstazAI.Application.Common.Interfaces;
using UstazAI.Domain.Services;

namespace UstazAI.Application.Dtos;

public sealed record StudyGuideStepDto(
    int StepNumber, string Title, string Description, int EstimatedMinutes, string VideoSearchQuery, string YouTubeSearchUrl);

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
