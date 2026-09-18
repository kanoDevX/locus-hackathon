using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UstazAI.Application.Ai;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Infrastructure.Ai;
using UstazAI.Infrastructure.Auth;
using UstazAI.Infrastructure.Maps;
using UstazAI.Infrastructure.Persistence;

namespace UstazAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<UstazDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<UstazDbContext>());

        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        // Resilience: retry + timeout + circuit breaker around every Gemini call, so a rate
        // limit or transient network blip during the live demo triggers the deterministic
        // fallback path instead of hanging the request (§2 Resilience).
        // Tuned from live telemetry (/system/insights): Gemini's real p95 latency is ~16s, so the
        // old hardcoded 15s attempt timeout cut off legitimately-slow successes, and a breaker that
        // opened after just 2 calls at 50% failure meant one pair of timeouts (easy, since
        // recommendations fire 5 narration calls concurrently) blocked every AI call for the whole
        // sampling window — turning a latency problem into a total outage. GeminiOptions.TimeoutSeconds
        // existed but was never read; it now drives the attempt timeout.
        var geminiTimeout = TimeSpan.FromSeconds(Math.Max(10, configuration.GetSection(GeminiOptions.SectionName).GetValue<int?>(nameof(GeminiOptions.TimeoutSeconds)) ?? 30));
        services.AddHttpClient<IAiReasoningService, GeminiReasoningService>()
            .AddStandardResilienceHandler(o =>
            {
                // 429 is a spent quota, not a transient blip: retrying it just burns time (and
                // trips the breaker); GeminiReasoningService moves to the next model instead.
                o.Retry.ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Exception is not null || args.Outcome.Result is { StatusCode: >= System.Net.HttpStatusCode.InternalServerError or System.Net.HttpStatusCode.RequestTimeout });
                o.CircuitBreaker.ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Exception is not null || args.Outcome.Result is { StatusCode: >= System.Net.HttpStatusCode.InternalServerError });
                o.Retry.MaxRetryAttempts = 1;
                o.Retry.Delay = TimeSpan.FromSeconds(1);
                o.AttemptTimeout.Timeout = geminiTimeout;
                o.TotalRequestTimeout.Timeout = geminiTimeout * 2 + TimeSpan.FromSeconds(5);
                o.CircuitBreaker.SamplingDuration = geminiTimeout * 3;
                o.CircuitBreaker.MinimumThroughput = 6;
                o.CircuitBreaker.FailureRatio = 0.7;
                o.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
            });

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IMapProvider, DeterministicMapProvider>();

        return services;
    }
}
