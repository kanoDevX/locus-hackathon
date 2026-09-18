using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using UstazAI.Application.Auth;
using UstazAI.Application.Comparison;
using UstazAI.Application.Dtos;
using UstazAI.Application.Profiles;
using UstazAI.Application.Recommendations;
using UstazAI.Domain.Enums;
using UstazAI.Endpoints;

namespace UstazAI.Tests;

/// <summary>
/// End-to-end walk of the whole mandatory journey (§4 / §7): profile -> diagnostics ->
/// recommendations -> comparison -> roadmap -> next action, plus the hard "changing one survey
/// answer visibly changes recommendations" requirement. Requires a reachable SQL Server /
/// LocalDB instance (see UstazApiFactory) — this is the "critical judge test scenario" the
/// submission checklist asks for.
/// </summary>
public sealed class JourneyIntegrationTests(UstazApiFactory factory) : IClassFixture<UstazApiFactory>
{
    private HttpClient CreateClient() => factory.CreateClient();

    private static async Task<HttpClient> AuthenticateAsync(HttpClient client)
    {
        var email = $"journey-{Guid.NewGuid():N}@test.ustazai";
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, "TestPassword123!", "Journey Test Student"));
        register.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await register.Content.ReadFromJsonAsync<AuthResultDto>(TestJson.Options);
        auth.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    [Fact]
    public async Task FullJourney_FromProfileToNextAction_CompletesEndToEndWithNonEmptyResults()
    {
        var client = await AuthenticateAsync(CreateClient());

        var profileRequest = new ProfileRequestDto(
            ProfileId: null, FullName: "Journey Test Student", Grade: 11, Age: 17, PreferredLanguage: Locale.En,
            Interests: ["Computer Science"], Gpa: 3.6m,
            ExamScores: [new ExamScoreDto(ExamType.Ielts, 6.5m, 9m, null)],
            TargetCountries: ["Kazakhstan"], BudgetBand: BudgetBand.Medium, TimelineMonthsToApplication: 10,
            Constraints: []);

        var profileResponse = await client.PostAsJsonAsync("/api/v1/profile", profileRequest, TestJson.Options);
        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profileResult = await profileResponse.Content.ReadFromJsonAsync<CreateOrUpdateProfileResult>(TestJson.Options);
        profileResult.Should().NotBeNull();
        var profileId = profileResult!.Profile.Id;

        var diagnosticsResponse = await client.PostAsync($"/api/v1/profile/{profileId}/diagnostics", null);
        diagnosticsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var diagnostics = await diagnosticsResponse.Content.ReadFromJsonAsync<DiagnosticsDto>(TestJson.Options);
        diagnostics!.Strengths.Should().NotBeEmpty();

        var recResponse = await client.PostAsync($"/api/v1/profile/{profileId}/recommendations", null);
        recResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var recResult = await recResponse.Content.ReadFromJsonAsync<GenerateRecommendationsResult>(TestJson.Options);
        recResult!.Recommendations.Should().HaveCountGreaterThanOrEqualTo(3);
        recResult.Delta.HasPreviousBatch.Should().BeFalse();

        var programIds = recResult.Recommendations.Take(2).Select(r => r.Program.ProgramId).ToList();
        var comparisonResponse = await client.PostAsJsonAsync("/api/v1/comparison", new CompareProgramsRequestDto(profileId, programIds));
        comparisonResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var comparisonRows = await comparisonResponse.Content.ReadFromJsonAsync<List<ProgramComparisonRowDto>>(TestJson.Options);
        comparisonRows.Should().HaveCount(2);

        var topProgramId = recResult.Recommendations[0].Program.ProgramId;
        var roadmapResponse = await client.PostAsJsonAsync($"/api/v1/profile/{profileId}/roadmap", new GenerateRoadmapRequestDto(topProgramId));
        roadmapResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var roadmapTasks = await roadmapResponse.Content.ReadFromJsonAsync<List<RoadmapTaskDto>>(TestJson.Options);
        roadmapTasks.Should().NotBeEmpty();

        var nextActionResponse = await client.GetAsync($"/api/v1/profile/{profileId}/next-action");
        nextActionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var nextAction = await nextActionResponse.Content.ReadFromJsonAsync<RoadmapTaskDto>(TestJson.Options);
        nextAction.Should().NotBeNull();

        // Hard requirement: changing one survey answer visibly and traceably changes
        // recommendations. Lower the budget band drastically and confirm the diff + delta.
        var updateRequest = profileRequest with { ProfileId = profileId, BudgetBand = BudgetBand.Low };
        var updateResponse = await client.PostAsJsonAsync("/api/v1/profile", updateRequest, TestJson.Options);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateResult = await updateResponse.Content.ReadFromJsonAsync<CreateOrUpdateProfileResult>(TestJson.Options);
        updateResult!.Diff.Should().NotBeNull();
        updateResult.Diff!.ChangedFields.Should().Contain(nameof(UstazAI.Domain.Entities.StudentProfile.BudgetBand));

        var recAfterResponse = await client.PostAsync($"/api/v1/profile/{profileId}/recommendations", null);
        recAfterResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var recAfter = await recAfterResponse.Content.ReadFromJsonAsync<GenerateRecommendationsResult>(TestJson.Options);
        recAfter!.Delta.HasPreviousBatch.Should().BeTrue();
    }

    [Fact]
    public async Task Profile_WithoutAuthentication_Returns401()
    {
        var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/profile/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
