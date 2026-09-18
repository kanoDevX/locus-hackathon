using UstazAI.Domain.Common;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Domain.Entities;

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
