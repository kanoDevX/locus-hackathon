using UstazAI.Domain.Common;
using UstazAI.Domain.Enums;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Domain.Entities;

public sealed class RoadmapTask : AuditableEntity<int>
{
    public Guid StudentProfileId { get; set; }
    public int? ProgramId { get; set; }

    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public RoadmapTaskCategory Category { get; set; }
    public DateOnly? DueDate { get; set; }
    public RoadmapTaskStatus Status { get; set; } = RoadmapTaskStatus.NotStarted;

    public int UrgencyScore { get; set; }
    public int ImpactScore { get; set; }

    public bool IsAiGenerated { get; set; }

    public string? Subject { get; set; }

    public List<ResourceLink> Resources { get; set; } = [];

    public List<RoadmapTaskDependency> Prerequisites { get; set; } = [];
}

public sealed class RoadmapTaskDependency : AuditableEntity<int>
{
    public int TaskId { get; set; }
    public RoadmapTask Task { get; set; } = default!;
    public int PrerequisiteTaskId { get; set; }
    public RoadmapTask PrerequisiteTask { get; set; } = default!;
}
