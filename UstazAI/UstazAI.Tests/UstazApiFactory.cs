using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace UstazAI.Tests;

/// <summary>
/// Points the app at a dedicated integration-test database and a throwaway JWT signing key so
/// the end-to-end journey test (see JourneyIntegrationTests) never depends on developer
/// user-secrets or a real Gemini key — it only needs a reachable SQL Server instance (matches
/// the dev connection in appsettings.json: a local named instance, e.g. SQL Server Express).
/// </summary>
public sealed class UstazApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] =
                    "Server=localhost\\SQLEXPRESS;Database=UztazAIDb_IntegrationTests;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true",
                ["Jwt:SigningKey"] = "Integration-Test-Signing-Key-Not-For-Production-Use-123!",
                ["Gemini:ApiKey"] = "",
                // The whole suite registers many accounts in quick succession from the same
                // loopback IP — that's legitimate test traffic, not credential stuffing, so the
                // production auth rate limit (see Program.cs / appsettings.json) is raised here.
                ["RateLimiting:AuthPermitLimit"] = "1000",
                ["RateLimiting:AuthWindowSeconds"] = "60"
            });
        });
    }
}
