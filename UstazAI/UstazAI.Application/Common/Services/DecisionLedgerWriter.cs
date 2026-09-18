using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;
using UstazAI.Domain.Services;

namespace UstazAI.Application.Common.Services;

public sealed class DecisionLedgerWriter(IAppDbContext db)
{
    public async Task AppendAsync(Guid profileId, AiDecisionType type, object payload, CancellationToken ct)
    {
        var last = await db.AiDecisionLogs
            .Where(l => l.StudentProfileId == profileId)
            .OrderByDescending(l => l.Id)
            .FirstOrDefaultAsync(ct);

        var previousHash = last?.Hash ?? DecisionLedger.GenesisHash;
        var json = JsonSerializer.Serialize(payload);
        var hash = DecisionLedger.ComputeHash(previousHash, json);

        db.AiDecisionLogs.Add(new AiDecisionLog
        {
            StudentProfileId = profileId,
            DecisionType = type,
            PayloadJson = json,
            PreviousHash = previousHash,
            Hash = hash
        });
    }
}
