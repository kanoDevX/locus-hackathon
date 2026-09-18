using System.Security.Cryptography;
using System.Text;

namespace UstazAI.Domain.Services;

/// <summary>
/// Hash-chaining helper for the append-only AiDecisionLog (§10.9). Each entry's hash covers its
/// own payload plus the previous entry's hash, so tampering with any historical row breaks the
/// chain from that point forward and can be detected by re-walking it.
/// </summary>
public static class DecisionLedger
{
    public const string GenesisHash = "GENESIS";

    public static string ComputeHash(string previousHash, string payloadJson)
    {
        var input = $"{previousHash}|{payloadJson}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
