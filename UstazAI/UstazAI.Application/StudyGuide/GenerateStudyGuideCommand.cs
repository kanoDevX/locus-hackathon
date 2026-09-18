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
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Application.StudyGuide;

public sealed record GenerateStudyGuideCommand(Guid ProfileId, Guid UserId, int RoadmapTaskId, bool ForceRegenerate = false)
    : IRequest<StudyGuideDto>;

public sealed class GenerateStudyGuideHandler(IAppDbContext db, IAiReasoningService ai, DecisionLedgerWriter ledger, ICurrentUser user, ILogger<GenerateStudyGuideHandler> logger)
    : IRequestHandler<GenerateStudyGuideCommand, StudyGuideDto>
{
    public async Task<StudyGuideDto> Handle(GenerateStudyGuideCommand cmd, CancellationToken ct)
    {
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(p => p.Id == cmd.ProfileId && p.UserId == cmd.UserId, ct)
            ?? throw new KeyNotFoundException($"Profile {cmd.ProfileId} not found");

        var task = await db.RoadmapTasks.FirstOrDefaultAsync(t => t.Id == cmd.RoadmapTaskId && t.StudentProfileId == profile.Id, ct)
            ?? throw new KeyNotFoundException($"Roadmap task {cmd.RoadmapTaskId} not found");

        var topic = string.IsNullOrWhiteSpace(task.Subject) ? task.Title : task.Subject;

        var existing = await db.StudyGuides.FirstOrDefaultAsync(g => g.RoadmapTaskId == cmd.RoadmapTaskId, ct);
        if (existing is not null && !cmd.ForceRegenerate)
            return existing.ToDto();

        var examRecord = await db.ExamRecords
            .Where(r => r.StudentProfileId == profile.Id)
            .OrderByDescending(r => r.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
        var subjectScore = examRecord?.SubjectBreakdown.FirstOrDefault(s => s.SubjectName == task.Subject);

        StudyGuideAiOutput output;
        try
        {
            output = await ai.GenerateStudyGuideAsync(new StudyGuideAiInput(
                topic,
                subjectScore?.Score,
                subjectScore?.MaxScore,
                subjectScore is null ? null : subjectScore.MaxScore - subjectScore.Score,
                user.UiLocale ?? profile.PreferredLanguage,
                task.Category.ToString(),
                task.Description), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI study guide generation failed for task {TaskId}; using deterministic fallback", cmd.RoadmapTaskId);
            output = BuildFallback(topic, task.Category);
        }

        var steps = output.Steps.Select((s, i) => new StudyGuideStep
        {
            StepNumber = i + 1,
            Title = s.Title,
            Description = s.Description,
            EstimatedMinutes = s.EstimatedMinutes,
            VideoSearchQuery = s.VideoSearchQuery
        }).ToList();

        var provenance = output.FallbackUsed
            ? DataProvenance.Demo("Deterministic fallback — AI narration unavailable")
            : DataProvenance.Demo("UstazAI study guide generator (Gemini-narrated, grounded in your own exam record where available)");

        Domain.Entities.StudyGuide guide;
        if (existing is not null)
        {
            existing.Steps = steps;
            existing.IsAiGenerated = !output.FallbackUsed;
            existing.FallbackUsed = output.FallbackUsed;
            existing.Provenance = provenance;
            guide = existing;
        }
        else
        {
            guide = new Domain.Entities.StudyGuide
            {
                StudentProfileId = profile.Id,
                RoadmapTaskId = task.Id,
                Subject = topic,
                Steps = steps,
                IsAiGenerated = !output.FallbackUsed,
                FallbackUsed = output.FallbackUsed,
                Provenance = provenance
            };
            db.StudyGuides.Add(guide);
        }

        await ledger.AppendAsync(profile.Id, AiDecisionType.StudyGuide, new { Subject = topic, cmd.RoadmapTaskId, output.FallbackUsed }, ct);
        await db.SaveChangesAsync(ct);

        return guide.ToDto();
    }

    private static StudyGuideAiOutput BuildFallback(string subject, RoadmapTaskCategory category) => category switch
    {
        RoadmapTaskCategory.Document => Steps(
            ("List every required document", $"Open the official admissions page of your target university and write down every document required for: {subject}.", 20, "how to apply to university abroad documents checklist"),
            ("Request originals early", "Ask your school or college office for the transcript and certificates now — official copies and translations often take weeks.", 30, "how to get school transcript apostille translation"),
            ("Get recommendation letters", "Ask two teachers who know you well, give them a short list of your achievements and the deadline.", 30, "how to ask teacher for recommendation letter"),
            ("Scan and check everything", "Scan documents in the required format, name files clearly and check each against the university's list.", 25, "university application documents scan format")),
        RoadmapTaskCategory.Financial => Steps(
            ("Check the official scholarship page", $"Read the university's own scholarship page for: {subject} — note coverage, eligibility and deadline exactly as published.", 25, "how to find university scholarships abroad"),
            ("Check your eligibility honestly", "Compare each requirement with your GPA, exam scores and documents; note what is missing.", 20, "scholarship eligibility requirements checklist"),
            ("Prepare the scholarship materials", "Draft the motivation letter and gather any extra documents the scholarship asks for.", 60, "how to write scholarship motivation letter"),
            ("Submit before the deadline", "Submit alongside your admission application and keep a confirmation copy.", 15, "scholarship application submit tips")),
        RoadmapTaskCategory.Interview => Steps(
            ("Understand what is expected", $"Re-read the program's essay prompt or interview format for: {subject}.", 20, "university admission essay how to start"),
            ("Outline your story", "List 3 experiences that show your motivation and pick the one strongest thread.", 30, "motivation letter structure examples"),
            ("Write and revise", "Write a first draft, then cut it and revise for clarity; get feedback from a teacher.", 90, "how to write personal statement"),
            ("Practice out loud", "Rehearse answers to common interview questions with a friend, aloud and timed.", 45, "university admission interview questions practice")),
        RoadmapTaskCategory.Application => Steps(
            ("Re-check the requirements", $"Go through the university's final checklist for: {subject} and tick off each item.", 20, "university application final checklist"),
            ("Fill in the application form", "Complete every field carefully; save a draft and re-read before moving on.", 45, "how to fill university application form"),
            ("Upload documents and pay fees", "Upload the prepared files and pay any application fee; keep receipts.", 20, "university application upload documents fee"),
            ("Submit and confirm", "Submit before the hard deadline, then save the confirmation and track the status portal.", 15, "after submitting university application what next")),
        RoadmapTaskCategory.Exam => Steps(
            ("Know the exam format", $"Study the official format and scoring of: {subject}, so you know what each section tests.", 30, "exam format explained scoring"),
            ("Take a diagnostic test", "Take one full practice test to find your weakest section.", 120, "full practice test free"),
            ("Train your weakest sections", "Spend most study time on the two weakest sections with targeted exercises.", 90, "exam preparation tips weak sections"),
            ("Register and plan test day", "Register on the official site (check fees and dates there) and plan travel and documents for test day.", 20, "how to register for exam step by step")),
        _ => BuildSubjectFallback(subject)
    };

    private static StudyGuideAiOutput Steps(params (string Title, string Description, int Minutes, string Query)[] steps) =>
        new([.. steps.Select(s => new StudyGuideStepAiOutput(s.Title, s.Description, s.Minutes, s.Query))], true);

    private static StudyGuideAiOutput BuildSubjectFallback(string subject) => new(
        [
            new StudyGuideStepAiOutput(
                $"Review {subject} fundamentals",
                $"Go through your class notes and textbook chapters on {subject}, and write down anything that's still unclear before moving on.",
                45, $"{subject} basics explained"),
            new StudyGuideStepAiOutput(
                "Work through practice problems",
                $"Solve a structured set of {subject} problems, starting with the easier ones and working up to harder ones.",
                60, $"{subject} practice problems"),
            new StudyGuideStepAiOutput(
                "Review your mistakes",
                "Go back over anything you got wrong and understand exactly why — not just what the correct answer was.",
                30, $"{subject} common mistakes"),
            new StudyGuideStepAiOutput(
                "Take a timed mock test",
                $"Simulate real exam conditions with a full {subject} practice test under a time limit.",
                90, $"{subject} mock exam practice test")
        ], true);
}
