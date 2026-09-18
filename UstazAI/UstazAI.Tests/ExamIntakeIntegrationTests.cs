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

/// <summary>Covers the new front door (§12): exam intake -> eligibility calculation -> result,
/// across the school/StandardEnt path and the college/ContinuingSpecialtyPaid path, plus the
/// validation guarding the branch-specific rules.</summary>
public sealed class ExamIntakeIntegrationTests(UstazApiFactory factory) : IClassFixture<UstazApiFactory>
{
    private async Task<(HttpClient Client, Guid ProfileId)> SetupAsync()
    {
        var client = factory.CreateClient();
        var email = $"examintake-{Guid.NewGuid():N}@test.ustazai";
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, "TestPassword123!", "Exam Intake Test Student"));
        var auth = await register.Content.ReadFromJsonAsync<AuthResultDto>(TestJson.Options);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var profileRequest = new ProfileRequestDto(
            ProfileId: null, FullName: "Exam Intake Test Student", Grade: 11, Age: 17, PreferredLanguage: Locale.En,
            Interests: ["Computer Science"], Gpa: 3.6m, ExamScores: [], TargetCountries: ["Kazakhstan"],
            BudgetBand: BudgetBand.Medium, TimelineMonthsToApplication: 10, Constraints: []);
        var profileResponse = await client.PostAsJsonAsync("/api/v1/profile", profileRequest, TestJson.Options);
        var profileResult = await profileResponse.Content.ReadFromJsonAsync<CreateOrUpdateProfileResult>(TestJson.Options);

        return (client, profileResult!.Profile.Id);
    }

    [Fact]
    public async Task SchoolStandardEnt_IntakeThenCalculate_ReturnsEligibilityAcrossDomesticPrograms()
    {
        var (client, profileId) = await SetupAsync();

        var intakeRequest = new ExamIntakeRequestDto(
            EducationStage.SchoolGrade11, AdmissionExamTrack.StandardEnt,
            [new SubjectScoreInput("Mathematics", 26, 35), new SubjectScoreInput("Physics", 24, 35)],
            TotalScore: 110, ExamDateTaken: null, CollegeBackground: null, SupplementaryExams: [],
            FundingTrackPreference.Flexible);

        var intakeResponse = await client.PostAsJsonAsync($"/api/v1/profile/{profileId}/exam-intake", intakeRequest, TestJson.Options);
        intakeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var intakeResult = await intakeResponse.Content.ReadFromJsonAsync<ExamIntakeResultDto>(TestJson.Options);
        intakeResult!.LatestExamRecord!.TotalScore.Should().Be(110);
        intakeResult.Diff.Should().NotBeNull();
        intakeResult.Diff!.LikelyAffectedStages.Should().Contain("Recommendations");

        var calculateResponse = await client.PostAsync($"/api/v1/profile/{profileId}/exam-intake/calculate", null);
        calculateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var verdicts = await calculateResponse.Content.ReadFromJsonAsync<List<EligibilityResultDto>>(TestJson.Options);
        verdicts.Should().NotBeEmpty("the seeded catalog has several domestic Kazakhstan programs");
        verdicts!.Should().OnlyContain(v => v.Program.Country == "Kazakhstan");
        verdicts.Should().OnlyContain(v => !v.IsDocumentOnlyVerdict, "StandardEnt is a scored track");

        var resultResponse = await client.GetAsync($"/api/v1/profile/{profileId}/exam-intake/result");
        resultResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var persisted = await resultResponse.Content.ReadFromJsonAsync<List<EligibilityResultDto>>(TestJson.Options);
        persisted.Should().HaveCount(verdicts.Count, "GET should return the same batch just calculated");
    }

    [Fact]
    public async Task CollegeContinuingSpecialtyPaid_NeedsNoScore_ReturnsDocumentOnlyVerdictEverywhere()
    {
        var (client, profileId) = await SetupAsync();

        var intakeRequest = new ExamIntakeRequestDto(
            EducationStage.CollegeStudent, AdmissionExamTrack.ContinuingSpecialtyPaid,
            SubjectBreakdown: [], TotalScore: null, ExamDateTaken: null,
            CollegeBackground: new CollegeBackgroundInput("Information Systems", 3.7m, 2025, TargetSpecialtyMatchesCollegeSpecialty: true),
            SupplementaryExams: [], FundingTrackPreference.PaidOnly);

        var intakeResponse = await client.PostAsJsonAsync($"/api/v1/profile/{profileId}/exam-intake", intakeRequest, TestJson.Options);
        intakeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var calculateResponse = await client.PostAsync($"/api/v1/profile/{profileId}/exam-intake/calculate", null);
        calculateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var verdicts = await calculateResponse.Content.ReadFromJsonAsync<List<EligibilityResultDto>>(TestJson.Options);

        verdicts.Should().NotBeEmpty();
        verdicts!.Should().OnlyContain(v => v.IsDocumentOnlyVerdict);
        verdicts.Should().OnlyContain(v => v.PaidTrackEligible);
        verdicts.Should().OnlyContain(v => v.Notes.Contains("admission commission"));
    }

    [Fact]
    public async Task SchoolNotTakingEnt_NeedsNoScore_ReturnsNoDomesticVerdictsWithoutError()
    {
        var (client, profileId) = await SetupAsync();

        var intakeRequest = new ExamIntakeRequestDto(
            EducationStage.SchoolGrade11, AdmissionExamTrack.NotTakingEnt,
            SubjectBreakdown: [], TotalScore: null, ExamDateTaken: null, CollegeBackground: null,
            SupplementaryExams: [new SupplementaryExamInput(ExamType.Sat, 1400, 1600, null)], FundingTrackPreference.Flexible);

        var intakeResponse = await client.PostAsJsonAsync($"/api/v1/profile/{profileId}/exam-intake", intakeRequest, TestJson.Options);
        intakeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Kazakhstan's ENT/grant system is inapplicable by definition for this track, not merely
        // unmet — the domestic-verdict list comes back empty (not an error), and the caller's job
        // is to route these students to Recommendations instead (country-agnostic, no ExamRecord
        // dependency), never a fabricated "failed the ENT" reading.
        var calculateResponse = await client.PostAsync($"/api/v1/profile/{profileId}/exam-intake/calculate", null);
        calculateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var verdicts = await calculateResponse.Content.ReadFromJsonAsync<List<EligibilityResultDto>>(TestJson.Options);
        verdicts.Should().BeEmpty();
    }

    [Fact]
    public async Task MismatchedTrackForEducationStage_IsRejectedWithValidationError()
    {
        var (client, profileId) = await SetupAsync();

        // A school-grade applicant can never submit a college-only track.
        var invalidRequest = new ExamIntakeRequestDto(
            EducationStage.SchoolGrade11, AdmissionExamTrack.ContinuingSpecialtyGrant,
            SubjectBreakdown: [], TotalScore: 30, ExamDateTaken: null, CollegeBackground: null,
            SupplementaryExams: [], FundingTrackPreference.GrantOnly);

        var response = await client.PostAsJsonAsync($"/api/v1/profile/{profileId}/exam-intake", invalidRequest, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CalculateWithoutSubmittingIntakeFirst_ReturnsConflict()
    {
        var (client, profileId) = await SetupAsync();

        var response = await client.PostAsync($"/api/v1/profile/{profileId}/exam-intake/calculate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
