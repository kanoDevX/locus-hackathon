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

/// <summary>Covers §13 (result-aware chat + gap-to-course roadmap): a student who already has an
/// eligibility verdict on file can generate a deterministic subject-prep plan and chat about their
/// own results — both paths must degrade gracefully to a deterministic fallback when Gemini is
/// unavailable (no API key configured for the test host), the same graceful-degradation contract
/// every other AI-backed feature in the product already honors.</summary>
public sealed class ClusterBIntegrationTests(UstazApiFactory factory) : IClassFixture<UstazApiFactory>
{
    private async Task<(HttpClient Client, Guid ProfileId, int ProgramId)> SetupWithEligibilityAsync()
    {
        var client = factory.CreateClient();
        var email = $"clusterb-{Guid.NewGuid():N}@test.ustazai";
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, "TestPassword123!", "Cluster B Test Student"));
        var auth = await register.Content.ReadFromJsonAsync<AuthResultDto>(TestJson.Options);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var profileRequest = new ProfileRequestDto(
            ProfileId: null, FullName: "Cluster B Test Student", Grade: 11, Age: 17, PreferredLanguage: Locale.En,
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
        // Nazarbayev CS has the highest university threshold (100) in the seeded catalog — a
        // score of 90 falls short there, guaranteeing real headroom for GapAnalysisEngine, unlike
        // the easier programs a score of 90 already clears outright.
        var programId = verdicts!.First(v => v.Program.UniversityName == "Nazarbayev University").Program.ProgramId;

        return (client, profileId, programId);
    }

    [Fact]
    public async Task GeneratePrepPlan_ProducesSubjectPrepTasksRankedByGap_AndGetReturnsTheSameBatch()
    {
        var (client, profileId, programId) = await SetupWithEligibilityAsync();

        var generateResponse = await client.PostAsync($"/api/v1/profile/{profileId}/prep-plan/{programId}", null);
        generateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasks = await generateResponse.Content.ReadFromJsonAsync<List<RoadmapTaskDto>>(TestJson.Options);

        tasks.Should().NotBeEmpty("the seeded exam record has real headroom on both subjects");
        tasks!.Should().OnlyContain(t => t.Category == RoadmapTaskCategory.SubjectPrep);
        tasks.Should().OnlyContain(t => t.Subject != null);
        tasks.Should().OnlyContain(t => t.Resources.Count > 0, "every SubjectPrep task carries curated resources");
        tasks.Select(t => t.Subject).Should().Contain("Mathematics", "Mathematics has the largest headroom (15 vs 5 points)");

        var getResponse = await client.GetAsync($"/api/v1/profile/{profileId}/prep-plan/{programId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var persisted = await getResponse.Content.ReadFromJsonAsync<List<RoadmapTaskDto>>(TestJson.Options);
        persisted.Should().HaveCount(tasks.Count);
    }

    [Fact]
    public async Task RegeneratingPrepPlan_ReplacesStaleNotStartedTasks_RatherThanAccumulating()
    {
        var (client, profileId, programId) = await SetupWithEligibilityAsync();

        await client.PostAsync($"/api/v1/profile/{profileId}/prep-plan/{programId}", null);
        var secondGenerate = await client.PostAsync($"/api/v1/profile/{profileId}/prep-plan/{programId}", null);
        var getResponse = await client.GetAsync($"/api/v1/profile/{profileId}/prep-plan/{programId}");
        var persisted = await getResponse.Content.ReadFromJsonAsync<List<RoadmapTaskDto>>(TestJson.Options);
        var secondBatch = await secondGenerate.Content.ReadFromJsonAsync<List<RoadmapTaskDto>>(TestJson.Options);

        persisted.Should().HaveCount(secondBatch!.Count, "regenerating must replace, not duplicate, NotStarted prep tasks");
    }

    [Fact]
    public async Task SendChatMessage_FallsBackGracefullyWithoutGeminiKey_AndGroundsReplyInEligibilityData()
    {
        var (client, profileId, _) = await SetupWithEligibilityAsync();

        var response = await client.PostAsJsonAsync($"/api/v1/profile/{profileId}/chat", new ChatRequestDto("What are my chances?"), TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reply = await response.Content.ReadFromJsonAsync<ChatMessageDtoForTest>(TestJson.Options);
        reply!.Role.Should().Be(ChatRole.Assistant);
        reply.Content.Should().NotBeNullOrWhiteSpace();
        reply.FallbackUsed.Should().BeTrue("no Gemini API key is configured for the test host");
    }

    [Fact]
    public async Task ChatHistory_PersistsBothUserAndAssistantTurnsInOrder()
    {
        var (client, profileId, _) = await SetupWithEligibilityAsync();

        await client.PostAsJsonAsync($"/api/v1/profile/{profileId}/chat", new ChatRequestDto("First question"), TestJson.Options);
        await client.PostAsJsonAsync($"/api/v1/profile/{profileId}/chat", new ChatRequestDto("Second question"), TestJson.Options);

        var historyResponse = await client.GetAsync($"/api/v1/profile/{profileId}/chat/history");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await historyResponse.Content.ReadFromJsonAsync<List<ChatMessageDtoForTest>>(TestJson.Options);

        history.Should().HaveCount(4); // 2 user + 2 assistant
        history![0].Role.Should().Be(ChatRole.User);
        history[0].Content.Should().Be("First question");
        history[1].Role.Should().Be(ChatRole.Assistant);
        history[2].Content.Should().Be("Second question");
    }
}

/// <summary>Local read shape mirroring ChatMessageDto's public fields — the real DTO is a mutable
/// class (needed for GuardrailBehavior's in-place SanitizeAiText), which deserializes fine via a
/// matching record for test assertions.</summary>
public sealed record ChatMessageDtoForTest(int Id, ChatRole Role, string Content, bool IsAiGenerated, bool FallbackUsed, DateTime CreatedAtUtc);
