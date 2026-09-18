using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Dtos;
using UstazAI.Domain.Enums;

namespace UstazAI.Application.Calendar;

public sealed record GetCalendarQuery(Guid ProfileId, Guid UserId) : IRequest<List<CalendarEntryDto>>;

public sealed class GetCalendarHandler(IAppDbContext db) : IRequestHandler<GetCalendarQuery, List<CalendarEntryDto>>
{
    public async Task<List<CalendarEntryDto>> Handle(GetCalendarQuery query, CancellationToken ct)
    {
        var ownsProfile = await db.StudentProfiles.AnyAsync(p => p.Id == query.ProfileId && p.UserId == query.UserId, ct);
        if (!ownsProfile) throw new KeyNotFoundException($"Profile {query.ProfileId} not found");

        var entries = new List<CalendarEntryDto>();

        var tasks = await db.RoadmapTasks
            .Where(t => t.StudentProfileId == query.ProfileId && t.Status != RoadmapTaskStatus.Done && t.DueDate != null)
            .ToListAsync(ct);
        entries.AddRange(tasks.Select(t => new CalendarEntryDto("Task", t.Title, t.DueDate!.Value, t.Category.ToString(), t.ProgramId, t.Id)));

        var trackedProgramIdsFromTasks = tasks.Where(t => t.ProgramId.HasValue).Select(t => t.ProgramId!.Value);
        var favoriteProgramIds = await db.FavoritePrograms
            .Where(f => f.StudentProfileId == query.ProfileId)
            .Select(f => f.ProgramId)
            .ToListAsync(ct);

        var trackedProgramIds = trackedProgramIdsFromTasks.Concat(favoriteProgramIds).Distinct().ToList();
        var programs = await db.ProgramOfferings
            .Where(p => trackedProgramIds.Contains(p.Id))
            .ToListAsync(ct);
        entries.AddRange(programs.Select(p => new CalendarEntryDto(
            "ApplicationDeadline", $"Application deadline — {p.Name}", p.ApplicationDeadline, "Deadline", p.Id, null)));

        return [.. entries.OrderBy(e => e.Date)];
    }
}

public sealed record GetNotificationsQuery(Guid ProfileId, Guid UserId, int WithinDays = 14) : IRequest<List<CalendarEntryDto>>;

public sealed class GetNotificationsHandler(ISender sender) : IRequestHandler<GetNotificationsQuery, List<CalendarEntryDto>>
{
    public async Task<List<CalendarEntryDto>> Handle(GetNotificationsQuery query, CancellationToken ct)
    {
        var all = await sender.Send(new GetCalendarQuery(query.ProfileId, query.UserId), ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizon = today.AddDays(query.WithinDays);

        return [.. all.Where(e => e.Date >= today && e.Date <= horizon).OrderBy(e => e.Date)];
    }
}
