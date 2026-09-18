namespace UstazAI.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SigningKey { get; set; } = "";
    public string Issuer { get; set; } = "UstazAI";
    public string Audience { get; set; } = "UstazAI.Clients";
    public int AccessTokenMinutes { get; set; } = 30;
}
