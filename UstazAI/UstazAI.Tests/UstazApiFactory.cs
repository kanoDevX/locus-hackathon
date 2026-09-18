using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace UstazAI.Tests;

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
                ["RateLimiting:AuthPermitLimit"] = "1000",
                ["RateLimiting:AuthWindowSeconds"] = "60"
            });
        });
    }
}
