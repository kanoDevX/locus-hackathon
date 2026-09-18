using System.Security.Cryptography;
using System.Text;

namespace UstazAI.Domain.Services;

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
