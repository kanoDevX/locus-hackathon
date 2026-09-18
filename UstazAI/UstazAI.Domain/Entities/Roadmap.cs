using UstazAI.Domain.Common;
using UstazAI.Domain.Enums;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Domain.Entities;

/// <summary>
/// A single node in the roadmap DAG. Prerequisite edges are modelled explicitly via
/// <see cref="RoadmapTaskDependency"/> so the frontend can render a real dependency graph
/// instead of a flat checklist, and deadlines are backward-calculated from the linked
/// program's real application deadline.
/// </summary>
public sealed class RoadmapTask : AuditableEntity<int>
{
    public Guid StudentProfileId { get; set; }
    public int? ProgramId { get; set; }

    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public RoadmapTaskCategory Category { get; set; }
    public DateOnly? DueDate { get; set; }
    public RoadmapTaskStatus Status { get; set; } = RoadmapTaskStatus.NotStarted;

    /// <summary>Urgency (0-100) x impact (0-100) drives the single highlighted Next Action.</summary>
    public int UrgencyScore { get; set; }
    public int ImpactScore { get; set; }

    public bool IsAiGenerated { get; set; }

    /// <summary>Only set for Category == SubjectPrep — the weak subject this task addresses
    /// (§13, GapAnalysisEngine).</summary>
    public string? Subject { get; set; }

    /// <summary>Curated study resources for a SubjectPrep task, from ResourceLinkCatalog. Empty
    /// for every other category.</summary>
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
