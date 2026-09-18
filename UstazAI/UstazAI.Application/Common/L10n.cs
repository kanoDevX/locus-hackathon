using UstazAI.Domain.Enums;

namespace UstazAI.Application.Common;

/// <summary>Picks the string for the language the user is reading. Used by the deterministic
/// fallbacks that appear when AI is unavailable, so a Russian UI never shows English sentences.</summary>
public static class L10n
{
    public static string Tr(Locale locale, string en, string ru, string kk) =>
        locale switch { Locale.Ru => ru, Locale.Kk => kk, _ => en };
}
