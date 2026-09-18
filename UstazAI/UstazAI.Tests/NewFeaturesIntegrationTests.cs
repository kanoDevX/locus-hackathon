using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using UstazAI.Application.Auth;
using UstazAI.Application.Dtos;
using UstazAI.Application.Profiles;
using UstazAI.Application.Scholarships;
using UstazAI.Domain.Enums;
using UstazAI.Endpoints;

namespace UstazAI.Tests;

public sealed class NewFeaturesIntegrationTests(UstazApiFactory factory) : IClassFixture<UstazApiFactory>
{
    private async Task<(HttpClient Client, string RefreshToken, Guid ProfileId)> SetupAsync()
    {
        var client = factory.CreateClient();
        var email = $"features-{Guid.NewGuid():N}@test.ustazai";
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, "TestPassword123!", "Features Test Student"));
        var auth = await register.Content.ReadFromJsonAsync<AuthResultDto>(TestJson.Options);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var profileRequest = new ProfileRequestDto(
            ProfileId: null, FullName: "Features Test Student", Grade: 11, Age: 17, PreferredLanguage: Locale.En,
            Interests: ["Computer Science"], Gpa: 3.6m,
            ExamScores: [new ExamScoreDto(ExamType.Ielts, 6.5m, 9m, null)],
            TargetCountries: ["Kazakhstan"], BudgetBand: BudgetBand.Medium, TimelineMonthsToApplication: 10,
            Constraints: []);
        var profileResponse = await client.PostAsJsonAsync("/api/v1/profile", profileRequest, TestJson.Options);
        var profileResult = await profileResponse.Content.ReadFromJsonAsync<CreateOrUpdateProfileResult>(TestJson.Options);

        return (client, auth.RefreshToken, profileResult!.Profile.Id);
    }

    [Fact]
    public async Task Favorites_CanBeAddedListedAndRemoved()
    {
        var (client, _, profileId) = await SetupAsync();

        var searchResponse = await client.GetAsync("/api/v1/programs/search?country=Kazakhstan");
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var programs = await searchResponse.Content.ReadFromJsonAsync<List<ProgramSummaryDto>>(TestJson.Options);
        programs.Should().NotBeEmpty();
        var programId = programs![0].ProgramId;

        var addResponse = await client.PostAsJsonAsync($"/api/v1/profile/{profileId}/favorites", new AddFavoriteRequestDto(programId, "Top choice"));
        addResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listResponse = await client.GetAsync($"/api/v1/profile/{profileId}/favorites");
        var favorites = await listResponse.Content.ReadFromJsonAsync<List<FavoriteDto>>(TestJson.Options);
        favorites.Should().ContainSingle(f => f.Program.ProgramId == programId);

        var removeResponse = await client.DeleteAsync($"/api/v1/profile/{profileId}/favorites/{programId}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listAfterRemove = await client.GetAsync($"/api/v1/profile/{profileId}/favorites");
        (await listAfterRemove.Content.ReadFromJsonAsync<List<FavoriteDto>>(TestJson.Options))!.Should().BeEmpty();
    }

    [Fact]
    public async Task Calendar_AndNotifications_ReflectRoadmapDeadlines()
    {
        var (client, _, profileId) = await SetupAsync();
        await client.PostAsync($"/api/v1/profile/{profileId}/recommendations", null);

        var recResponse = await client.GetAsync($"/api/v1/profile/{profileId}/recommendations");
        var recs = await recResponse.Content.ReadFromJsonAsync<List<RecommendationDto>>(TestJson.Options);
        var topProgramId = recs![0].Program.ProgramId;

        await client.PostAsJsonAsync($"/api/v1/profile/{profileId}/roadmap", new GenerateRoadmapRequestDto(topProgramId));

        var calendarResponse = await client.GetAsync($"/api/v1/profile/{profileId}/calendar");
        calendarResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var calendar = await calendarResponse.Content.ReadFromJsonAsync<List<CalendarEntryDto>>(TestJson.Options);
        calendar.Should().NotBeEmpty();

        var icsResponse = await client.GetAsync($"/api/v1/profile/{profileId}/calendar.ics");
        icsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var icsText = await icsResponse.Content.ReadAsStringAsync();
        icsText.Should().Contain("BEGIN:VCALENDAR").And.Contain("BEGIN:VEVENT");
    }

    [Fact]
    public async Task ScholarshipSearch_ReturnsResultsFilteredByCoverage()
    {
        var (client, _, _) = await SetupAsync();

        var response = await client.GetAsync("/api/v1/scholarships/search?minCoverage=50");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<ScholarshipSearchResultDto>>(TestJson.Options);
        results.Should().NotBeEmpty();
        results!.Should().OnlyContain(r => r.CoveragePercent >= 50);
    }

    [Fact]
    public async Task EssayReview_WithoutGeminiKey_FallsBackToDeterministicFeedback()
    {
        var (client, _, profileId) = await SetupAsync();

        var essay = string.Join(" ", Enumerable.Repeat("This is a sentence about my personal journey and growth.", 40));
        var response = await client.PostAsJsonAsync($"/api/v1/profile/{profileId}/essays/review", new ReviewEssayRequestDto(essay, "Describe a challenge you overcame."));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var review = await response.Content.ReadFromJsonAsync<EssayReviewDto>(TestJson.Options);
        review!.FallbackUsed.Should().BeTrue();
        review.Strengths.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Logout_RevokesRefreshTokenSoItCannotBeReused()
    {
        var (client, refreshToken, _) = await SetupAsync();

        var logoutResponse = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken });
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
