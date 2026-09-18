namespace UstazAI.Infrastructure.Auth;

/// <summary>Bound from configuration — SigningKey must come from user-secrets/environment
/// variables, never committed to source control (see README).</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SigningKey { get; set; } = "";
    public string Issuer { get; set; } = "UstazAI";
    public string Audience { get; set; } = "UstazAI.Clients";
    public int AccessTokenMinutes { get; set; } = 30;
}
