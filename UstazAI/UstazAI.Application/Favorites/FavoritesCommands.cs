using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Dtos;
using UstazAI.Domain.Entities;

namespace UstazAI.Application.Favorites;

public sealed record AddFavoriteCommand(Guid ProfileId, Guid UserId, int ProgramId, string? Note) : IRequest<FavoriteDto>;

public sealed class AddFavoriteHandler(IAppDbContext db) : IRequestHandler<AddFavoriteCommand, FavoriteDto>
{
    public async Task<FavoriteDto> Handle(AddFavoriteCommand cmd, CancellationToken ct)
    {
        var ownsProfile = await db.StudentProfiles.AnyAsync(p => p.Id == cmd.ProfileId && p.UserId == cmd.UserId, ct);
        if (!ownsProfile) throw new KeyNotFoundException($"Profile {cmd.ProfileId} not found");

        var program = await db.ProgramOfferings.Include(p => p.University)
            .FirstOrDefaultAsync(p => p.Id == cmd.ProgramId, ct)
            ?? throw new KeyNotFoundException($"Program {cmd.ProgramId} not found");

        var existing = await db.FavoritePrograms
            .FirstOrDefaultAsync(f => f.StudentProfileId == cmd.ProfileId && f.ProgramId == cmd.ProgramId, ct);

        if (existing is not null)
        {
            existing.Note = cmd.Note;
            existing.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return new FavoriteDto(existing.Id, program.ToDto(), existing.Note, existing.CreatedAtUtc);
        }

        var favorite = new FavoriteProgram { StudentProfileId = cmd.ProfileId, ProgramId = cmd.ProgramId, Note = cmd.Note };
        db.FavoritePrograms.Add(favorite);
        await db.SaveChangesAsync(ct);

        return new FavoriteDto(favorite.Id, program.ToDto(), favorite.Note, favorite.CreatedAtUtc);
    }
}

public sealed record RemoveFavoriteCommand(Guid ProfileId, Guid UserId, int ProgramId) : IRequest;

public sealed class RemoveFavoriteHandler(IAppDbContext db) : IRequestHandler<RemoveFavoriteCommand>
{
    public async Task Handle(RemoveFavoriteCommand cmd, CancellationToken ct)
    {
        var ownsProfile = await db.StudentProfiles.AnyAsync(p => p.Id == cmd.ProfileId && p.UserId == cmd.UserId, ct);
        if (!ownsProfile) throw new KeyNotFoundException($"Profile {cmd.ProfileId} not found");

        var favorite = await db.FavoritePrograms
            .FirstOrDefaultAsync(f => f.StudentProfileId == cmd.ProfileId && f.ProgramId == cmd.ProgramId, ct);

        if (favorite is not null)
        {
            db.FavoritePrograms.Remove(favorite);
            await db.SaveChangesAsync(ct);
        }
    }
}

public sealed record GetFavoritesQuery(Guid ProfileId, Guid UserId) : IRequest<List<FavoriteDto>>;

public sealed class GetFavoritesHandler(IAppDbContext db) : IRequestHandler<GetFavoritesQuery, List<FavoriteDto>>
{
    public async Task<List<FavoriteDto>> Handle(GetFavoritesQuery query, CancellationToken ct)
    {
        var ownsProfile = await db.StudentProfiles.AnyAsync(p => p.Id == query.ProfileId && p.UserId == query.UserId, ct);
        if (!ownsProfile) throw new KeyNotFoundException($"Profile {query.ProfileId} not found");

        var favorites = await db.FavoritePrograms
            .Where(f => f.StudentProfileId == query.ProfileId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync(ct);

        var programIds = favorites.Select(f => f.ProgramId).ToList();
        var programs = await db.ProgramOfferings.Include(p => p.University)
            .Where(p => programIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        return [.. favorites
            .Where(f => programs.ContainsKey(f.ProgramId))
            .Select(f => new FavoriteDto(f.Id, programs[f.ProgramId].ToDto(), f.Note, f.CreatedAtUtc))];
    }
}
