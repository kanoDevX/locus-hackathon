using System.Text.RegularExpressions;

namespace UstazAI.Domain.Services;

/// <summary>
/// Rule set enforced on every AI-generated string before it reaches the client. Operationalizes
/// the case's "no fabricated certainty / no guaranteed admission" rule as code, not just a
/// prompt instruction (§5.3).
/// </summary>
public static partial class GuardrailRules
{
    private static readonly Regex[] ForbiddenPatterns =
    [
        ForbiddenGuarantee(),
        ForbiddenCertainty(),
        ForbiddenPromise()
    ];

    public static bool ContainsForbiddenLanguage(string text) =>
        ForbiddenPatterns.Any(p => p.IsMatch(text));

    /// <summary>Rewrites a violating sentence into a hedged, non-guaranteeing equivalent.
    /// Used as a last-resort safety net; the Gemini system instructions are already told never
    /// to produce this language in the first place.</summary>
    public static string Sanitize(string text)
    {
        if (!ContainsForbiddenLanguage(text)) return text;

        var sanitized = text;
        sanitized = GuaranteeWord().Replace(sanitized, "increases your estimated likelihood of");
        sanitized = CertaintyWord().Replace(sanitized, "based on available data, you appear");
        sanitized = PromiseWord().Replace(sanitized, "may help you get");
        return sanitized;
    }

    [GeneratedRegex(@"\b(guarantee[sd]?|guaranteed admission|100%\s*(chance|admission))\b", RegexOptions.IgnoreCase)]
    private static partial Regex ForbiddenGuarantee();

    [GeneratedRegex(@"\b(will definitely|certainly will|you will be admitted|no doubt you)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ForbiddenCertainty();

    [GeneratedRegex(@"\b(promise[sd]?|assured admission)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ForbiddenPromise();

    [GeneratedRegex(@"\b(guarantee[sd]?|guaranteed admission|100%\s*(chance|admission))\b", RegexOptions.IgnoreCase)]
    private static partial Regex GuaranteeWord();

    [GeneratedRegex(@"\b(will definitely|certainly will|you will be admitted|no doubt you)\b", RegexOptions.IgnoreCase)]
    private static partial Regex CertaintyWord();

    [GeneratedRegex(@"\b(promise[sd]?|assured admission)\b", RegexOptions.IgnoreCase)]
    private static partial Regex PromiseWord();
}
