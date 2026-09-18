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

        var geminiTimeout = TimeSpan.FromSeconds(Math.Max(10, configuration.GetSection(GeminiOptions.SectionName).GetValue<int?>(nameof(GeminiOptions.TimeoutSeconds)) ?? 30));
        services.AddHttpClient<IAiReasoningService, GeminiReasoningService>()
            .AddStandardResilienceHandler(o =>
            {
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
