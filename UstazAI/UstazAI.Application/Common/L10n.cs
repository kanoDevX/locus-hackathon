using UstazAI.Domain.Enums;

namespace UstazAI.Application.Common;

public static class L10n
{
    public static string Tr(Locale locale, string en, string ru, string kk) =>
        locale switch { Locale.Ru => ru, Locale.Kk => kk, _ => en };
}
