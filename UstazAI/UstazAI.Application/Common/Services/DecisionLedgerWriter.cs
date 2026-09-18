using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;
using UstazAI.Domain.Services;

namespace UstazAI.Application.Common.Services;

/// <summary>Appends one hash-chained row to the AiDecisionLog ledger (§10.9). Does not call
/// SaveChanges — the caller's unit of work (the MediatR handler) commits it alongside its own
/// changes so the decision record and the data it describes are persisted atomically.</summary>
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
