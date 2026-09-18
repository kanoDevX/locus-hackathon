using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Dtos;

namespace UstazAI.Application.Diagnostics;

public sealed record GenerateDiagnosticsCommand(Guid ProfileId, Guid UserId) : IRequest<DiagnosticsDto>;

public sealed class GenerateDiagnosticsHandler(IAppDbContext db, DiagnosticsService diagnosticsService)
    : IRequestHandler<GenerateDiagnosticsCommand, DiagnosticsDto>
{
    public async Task<DiagnosticsDto> Handle(GenerateDiagnosticsCommand cmd, CancellationToken ct)
    {
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(p => p.Id == cmd.ProfileId && p.UserId == cmd.UserId, ct)
            ?? throw new KeyNotFoundException($"Profile {cmd.ProfileId} not found");

        var diagnostics = await diagnosticsService.GenerateAsync(profile, ct);
        db.Diagnostics.Add(diagnostics);
        await db.SaveChangesAsync(ct);

        return diagnostics.ToDto();
    }
}
