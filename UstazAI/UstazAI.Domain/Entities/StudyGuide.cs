using UstazAI.Domain.Common;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Domain.Entities;

/// <summary>
/// A generated, persisted "how to actually learn this" plan for one roadmap task (§ UX request:
/// a full-screen, step-by-step study guide reachable from a roadmap task, saved so it can be
/// reopened later rather than regenerated — and cheaper on the Gemini budget besides). One
/// StudyGuide per RoadmapTask; regenerating replaces its Steps in place rather than
/// accumulating a history, since there is exactly one "current" guide a student is following.
/// </summary>
public sealed class StudyGuide : AuditableEntity<int>
{
    public Guid StudentProfileId { get; set; }
    public int RoadmapTaskId { get; set; }
    public string Subject { get; set; } = default!;

    public List<StudyGuideStep> Steps { get; set; } = [];

    public bool IsAiGenerated { get; set; }
    public bool FallbackUsed { get; set; }
    public DataProvenance Provenance { get; set; } = DataProvenance.Demo("UstazAI study guide generator");
}
