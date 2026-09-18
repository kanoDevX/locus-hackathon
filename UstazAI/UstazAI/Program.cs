using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using UstazAI.Application;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Auth;
using UstazAI.Endpoints;
using UstazAI.Infrastructure;
using UstazAI.Infrastructure.Auth;
using UstazAI.Infrastructure.Persistence;
using UstazAI.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/ustazai-.log", rollingInterval: RollingInterval.Day));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddMemoryCache();

// Enums serialize as readable strings ("Medium", not 2) over the wire — consistent with how
// they're already stored as strings in the database (see UstazDbContext), and far more legible
// for a frontend client and for judges reading the Scalar/OpenAPI docs directly.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<UstazDbContext>("database");

builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

// Bound from the same IOptions<JwtOptions> that JwtTokenService uses to *issue* tokens (both
// resolved lazily through DI), so the signing key used to validate a token always matches the
// one used to sign it — reading a manual snapshot here instead can desync from what the token
// service later resolves (e.g. under WebApplicationFactory config overrides in tests).
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<Microsoft.Extensions.Options.IOptions<JwtOptions>>((bearerOptions, jwtOptionsAccessor) =>
    {
        var jwtOptions = jwtOptionsAccessor.Value;
        bearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                string.IsNullOrWhiteSpace(jwtOptions.SigningKey) ? new string('0', 32) : jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

// Token-bucket rate limiting on AI-backed endpoints, keyed per authenticated user (falls back
// to remote IP), so one caller can't burn through the Gemini budget mid-demo (§3).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("ai", httpContext => RateLimitPartition.GetTokenBucketLimiter(
        partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        factory: _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 20,
            TokensPerPeriod = 5,
            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
            QueueLimit = 5,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        }));

    // Brute-force / credential-stuffing mitigation on login, register and refresh (OWASP ASVS
    // baseline for authentication endpoints) — keyed by IP since the caller isn't authenticated
    // yet at this point. Fixed window rather than a token bucket: a burst of guesses should be
    // blocked outright for the rest of the window, not smoothly drip-fed through. The limit is
    // read from IConfiguration via the request's own service provider (not a snapshot of
    // builder.Configuration captured at startup) so it reflects whatever configuration source
    // ends up winning once the host is fully built — a manual startup-time snapshot can desync
    // from that under WebApplicationFactory config overrides in tests (see UstazApiFactory),
    // the same class of bug already hit once with the JWT signing key.
    options.AddPolicy("auth", httpContext =>
    {
        var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var authPermitLimit = config.GetValue("RateLimiting:AuthPermitLimit", 10);
        var authWindowSeconds = config.GetValue("RateLimiting:AuthWindowSeconds", 60);
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authPermitLimit,
                Window = TimeSpan.FromSeconds(authWindowSeconds),
                QueueLimit = 0
            });
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UstazDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await DbSeeder.SeedAsync(db, hasher);
}

app.UseMiddleware<UstazAI.Middleware.ExceptionHandlingMiddleware>();
app.UseMiddleware<UstazAI.Middleware.CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("UstazAI API"));
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Standard infra-conventional health endpoint (for load balancers / uptime tooling), in
// addition to the richer /api/v1/system/health used by the product's own judge test script.
app.MapHealthChecks("/health").AllowAnonymous();

app.MapAuthEndpoints();
app.MapOnboardingEndpoints();
app.MapExamIntakeEndpoints();
app.MapChatEndpoints();
app.MapPrepPlanEndpoints();
app.MapStudyGuideEndpoints();
app.MapProfileEndpoints();
app.MapDiagnosticsEndpoints();
app.MapRecommendationsEndpoints();
app.MapComparisonEndpoints();
app.MapCampusMapEndpoints();
app.MapRoadmapEndpoints();
app.MapNextActionEndpoints();
app.MapWhatIfEndpoints();
app.MapPeerPathwaysEndpoints();
app.MapFavoritesEndpoints();
app.MapCalendarEndpoints();
app.MapScholarshipsEndpoints();
app.MapProgramsEndpoints();
app.MapEssaysEndpoints();
app.MapSystemEndpoints();

app.Run();

public partial class Program; // exposed for WebApplicationFactory in integration tests
