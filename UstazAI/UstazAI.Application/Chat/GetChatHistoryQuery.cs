using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Dtos;

namespace UstazAI.Application.Chat;

public sealed record GetChatHistoryQuery(Guid ProfileId, Guid UserId) : IRequest<List<ChatMessageDto>>;

public sealed class GetChatHistoryHandler(IAppDbContext db) : IRequestHandler<GetChatHistoryQuery, List<ChatMessageDto>>
{
    public async Task<List<ChatMessageDto>> Handle(GetChatHistoryQuery query, CancellationToken ct)
    {
        var ownsProfile = await db.StudentProfiles.AnyAsync(p => p.Id == query.ProfileId && p.UserId == query.UserId, ct);
        if (!ownsProfile) throw new KeyNotFoundException($"Profile {query.ProfileId} not found");

        var messages = await db.ChatMessages
            .Where(m => m.StudentProfileId == query.ProfileId)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(ct);

        return [.. messages.Select(m => m.ToDto())];
    }
}
