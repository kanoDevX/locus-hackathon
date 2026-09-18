using System.Text.RegularExpressions;

namespace UstazAI.Domain.Services;

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
