namespace UstazAI.Endpoints;

public static class OnboardingEndpoints
{
    public static void MapOnboardingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/onboarding").WithTags("Onboarding");

        group.MapGet("/value-proposition", (string? locale, IConfiguration config) =>
        {
            var loc = string.IsNullOrWhiteSpace(locale) ? "en" : locale.ToLowerInvariant();
            var section = config.GetSection($"Onboarding:ValueProposition:{loc}");
            if (!section.Exists())
                section = config.GetSection("Onboarding:ValueProposition:en");

            return Results.Ok(new
            {
                Locale = loc,
                Headline = section["Headline"],
                Body = section["Body"],
                ExpectedOutput = section.GetSection("ExpectedOutput").Get<string[]>() ?? []
            });
        }).AllowAnonymous();
    }
}
