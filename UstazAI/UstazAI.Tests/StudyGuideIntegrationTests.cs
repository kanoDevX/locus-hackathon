using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using UstazAI.Application.Auth;
using UstazAI.Application.Dtos;
using UstazAI.Application.ExamIntake;
using UstazAI.Application.Profiles;
using UstazAI.Domain.Enums;
using UstazAI.Endpoints;

namespace UstazAI.Tests;

/// <summary>Covers the full-screen study guide (§ UX request): generating a step-by-step plan
/// for a SubjectPrep roadmap task, persisting it so a second GET returns the same content rather
/// than regenerating, and confirming every step's "video" is a real YouTube search link built
/// from a search phrase — never a specific fabricated video (no video-metadata API is wired up,
/// see StudyGuideStep's own doc comment on the Domain side).</summary>
public sealed class StudyGuideIntegrationTests(UstazApiFactory factory) : IClassFixture<UstazApiFactory>
{
    private async Task<(HttpClient Client, Guid ProfileId, int TaskId)> SetupWithPrepTaskAsync()
    {
        var client = factory.CreateClient();
        var email = $"studyguide-{Guid.NewGuid():N}@test.ustazai";
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, "TestPassword123!", "Study Guide Test Student"));
        var auth = await register.Content.ReadFromJsonAsync<AuthResultDto>(TestJson.Options);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var profileRequest = new ProfileRequestDto(
            ProfileId: null, FullName: "Study Guide Test Student", Grade: 11, Age: 17, PreferredLanguage: Locale.En,
            Interests: ["Computer Science"], Gpa: 3.6m, ExamScores: [], TargetCountries: ["Kazakhstan"],
            BudgetBand: BudgetBand.Medium, TimelineMonthsToApplication: 10, Constraints: []);
        var profileResponse = await client.PostAsJsonAsync("/api/v1/profile", profileRequest, TestJson.Options);
        var profileResult = await profileResponse.Content.ReadFromJsonAsync<CreateOrUpdateProfileResult>(TestJson.Options);
        var profileId = profileResult!.Profile.Id;

        var intakeRequest = new ExamIntakeRequestDto(
            EducationStage.SchoolGrade11, AdmissionExamTrack.StandardEnt,
            [new SubjectScoreInput("Mathematics", 20, 35), new SubjectScoreInput("Physics", 30, 35)],
            TotalScore: 90, ExamDateTaken: null, CollegeBackground: null, SupplementaryExams: [],
            FundingTrackPreference.Flexible);
        await client.PostAsJsonAsync($"/api/v1/profile/{profileId}/exam-intake", intakeRequest, TestJson.Options);

        var calculateResponse = await client.PostAsync($"/api/v1/profile/{profileId}/exam-intake/calculate", null);
        var verdicts = await calculateResponse.Content.ReadFromJsonAsync<List<EligibilityResultDto>>(TestJson.Options);
        var programId = verdicts!.First(v => v.Program.UniversityName == "Nazarbayev University").Program.ProgramId;

        var prepPlanResponse = await client.PostAsync($"/api/v1/profile/{profileId}/prep-plan/{programId}", null);
        var prepTasks = await prepPlanResponse.Content.ReadFromJsonAsync<List<RoadmapTaskDto>>(TestJson.Options);
        var taskId = prepTasks!.First().TaskId;

        return (client, profileId, taskId);
    }

    [Fact]
    public async Task GenerateStudyGuide_ProducesOrderedStepsWithRealYouTubeSearchLinks_NeverFabricatedVideos()
    {
        var (client, profileId, taskId) = await SetupWithPrepTaskAsync();

        var response = await client.PostAsync($"/api/v1/profile/{profileId}/roadmap/tasks/{taskId}/study-guide", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var guide = await response.Content.ReadFromJsonAsync<StudyGuideDto>(TestJson.Options);
        guide!.Steps.Should().NotBeEmpty();
        guide.Steps.Select(s => s.StepNumber).Should().BeInAscendingOrder();
        guide.Steps.Should().OnlyContain(s => s.YouTubeSearchUrl.StartsWith("https://www.youtube.com/results?search_query="),
            "every step must link to a real YouTube search, never a specific invented video");
        // No Gemini key is configured for the test host, so this must be the deterministic
        // fallback — proving the feature degrades gracefully instead of failing outright.
        guide.FallbackUsed.Should().BeTrue();
    }

    [Fact]
    public async Task SecondGet_ReturnsTheSamePersistedGuide_RatherThanRegenerating()
    {
        var (client, profileId, taskId) = await SetupWithPrepTaskAsync();

        var firstResponse = await client.PostAsync($"/api/v1/profile/{profileId}/roadmap/tasks/{taskId}/study-guide", null);
        var first = await firstResponse.Content.ReadFromJsonAsync<StudyGuideDto>(TestJson.Options);

        var getResponse = await client.GetAsync($"/api/v1/profile/{profileId}/roadmap/tasks/{taskId}/study-guide");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<StudyGuideDto>(TestJson.Options);

        fetched!.Id.Should().Be(first!.Id);
        fetched.Steps.Should().BeEquivalentTo(first.Steps);
    }

    [Fact]
    public async Task GetBeforeAnyGeneration_ReturnsEmptyBody_NeverA404()
    {
        var (client, profileId, taskId) = await SetupWithPrepTaskAsync();

        var response = await client.GetAsync($"/api/v1/profile/{profileId}/roadmap/tasks/{taskId}/study-guide");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().BeEmpty();
    }

    [Fact]
    public async Task ForceRegenerate_ReplacesTheGuideInPlace_RatherThanCreatingASecondRow()
    {
        var (client, profileId, taskId) = await SetupWithPrepTaskAsync();

        var firstResponse = await client.PostAsync($"/api/v1/profile/{profileId}/roadmap/tasks/{taskId}/study-guide", null);
        var first = await firstResponse.Content.ReadFromJsonAsync<StudyGuideDto>(TestJson.Options);

        var regenerateResponse = await client.PostAsJsonAsync(
            $"/api/v1/profile/{profileId}/roadmap/tasks/{taskId}/study-guide",
            new GenerateStudyGuideRequestDto(ForceRegenerate: true), TestJson.Options);
        regenerateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var regenerated = await regenerateResponse.Content.ReadFromJsonAsync<StudyGuideDto>(TestJson.Options);

        regenerated!.Id.Should().Be(first!.Id, "regenerating replaces the one guide for this task, not a new row");
    }
}
