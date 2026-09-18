using System.Text;
using MediatR;
using UstazAI.Application.Calendar;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Dtos;

namespace UstazAI.Endpoints;

public static class CalendarEndpoints
{
    public static void MapCalendarEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/profile/{profileId:guid}").WithTags("Calendar").RequireAuthorization();

        group.MapGet("/calendar", async (Guid profileId, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetCalendarQuery(profileId, user.UserId!.Value), ct)));

        group.MapGet("/notifications", async (Guid profileId, int? withinDays, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetNotificationsQuery(profileId, user.UserId!.Value, withinDays ?? 14), ct)));

        group.MapGet("/calendar.ics", async (Guid profileId, ICurrentUser user, ISender sender, CancellationToken ct) =>
        {
            var entries = await sender.Send(new GetCalendarQuery(profileId, user.UserId!.Value), ct);
            var ics = BuildIcs(entries);
            return Results.Text(ics, "text/calendar", Encoding.UTF8);
        });
    }

    private static string BuildIcs(List<CalendarEntryDto> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//UstazAI//Admission Route//EN");

        foreach (var entry in entries)
        {
            var date = entry.Date.ToString("yyyyMMdd");
            var uid = $"{entry.Type}-{entry.RelatedTaskId}-{entry.RelatedProgramId}-{date}@ustazai";
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{uid}");
            sb.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
            sb.AppendLine($"DTSTART;VALUE=DATE:{date}");
            sb.AppendLine($"SUMMARY:{EscapeIcsText(entry.Title)}");
            if (!string.IsNullOrWhiteSpace(entry.Category))
                sb.AppendLine($"CATEGORIES:{EscapeIcsText(entry.Category)}");
            sb.AppendLine("END:VEVENT");
        }

        sb.AppendLine("END:VCALENDAR");
        return sb.ToString();
    }

    private static string EscapeIcsText(string text) =>
        text.Replace("\\", "\\\\").Replace(",", "\\,").Replace(";", "\\;");
}
