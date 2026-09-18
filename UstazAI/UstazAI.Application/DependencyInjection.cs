using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using UstazAI.Application.Common.Behaviors;
using UstazAI.Application.Common.Services;
using UstazAI.Application.Diagnostics;
using UstazAI.Application.Recommendations;
using UstazAI.Application.Roadmap;

namespace UstazAI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(GuardrailBehavior<,>));

        services.AddScoped<DecisionLedgerWriter>();
        services.AddScoped<RecommendationEngine>();
        services.AddScoped<RoadmapService>();
        services.AddScoped<DiagnosticsService>();

        return services;
    }
}
