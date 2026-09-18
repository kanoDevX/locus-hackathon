using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using UstazAI.Application.Auth;
using UstazAI.Application.Comparison;
using UstazAI.Application.Dtos;
using UstazAI.Application.Profiles;
using UstazAI.Domain.Enums;
using UstazAI.Endpoints;

namespace UstazAI.Tests;

public sealed class CampusMapIntegrationTests(UstazApiFactory factory) : IClassFixture<UstazApiFactory>
{
    private async Task<(HttpClient Client, Guid ProfileId)> SetupAsync()
    {
        var client = factory.CreateClient();
        var email = $"campusmap-{Guid.NewGuid():N}@test.ustazai";
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, "TestPassword123!", "Campus Map Test Student"));
        var auth = await register.Content.ReadFromJsonAsync<AuthResultDto>(TestJson.Options);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var profileRequest = new ProfileRequestDto(
            ProfileId: null, FullName: "Campus Map Test Student", Grade: 11, Age: 17, PreferredLanguage: Locale.En,
            Interests: ["Computer Science"], Gpa: 3.6m, ExamScores: [], TargetCountries: ["Kazakhstan", "Russia"],
            BudgetBand: BudgetBand.Medium, TimelineMonthsToApplication: 10, Constraints: []);
        var profileResponse = await client.PostAsJsonAsync("/api/v1/profile", profileRequest, TestJson.Options);
        var profileResult = await profileResponse.Content.ReadFromJsonAsync<CreateOrUpdateProfileResult>(TestJson.Options);

        return (client, profileResult!.Profile.Id);
    }

    private static async Task<int> FindProgramIdAsync(HttpClient client, string country, string programNameContains)
    {
        var response = await client.GetAsync($"/api/v1/programs/search?country={Uri.EscapeDataString(country)}");
        var programs = await response.Content.ReadFromJsonAsync<List<ProgramSummaryDto>>(TestJson.Options);
        return programs!.First(p => p.ProgramName.Contains(programNameContains, StringComparison.OrdinalIgnoreCase)).ProgramId;
    }

    [Fact]
    public async Task MappedUniversity_ReturnsCoordinatesEnvironmentAndTwoGisLink()
    {
        var (client, profileId) = await SetupAsync();
        var programId = await FindProgramIdAsync(client, "Kazakhstan", "Computer Science");

        var response = await client.GetAsync($"/api/v1/profile/{profileId}/campus-map/{programId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var map = await response.Content.ReadFromJsonAsync<CampusMapDto>(TestJson.Options);
        map!.Coordinates.Should().NotBeNull();
        map.Environment.Should().NotBeNull();
        map.MapLink.Should().NotBeNull();
        map.MapLink!.Provider.Should().Be("2GIS");
    }

    [Fact]
    public async Task UnmappedUniversity_ReturnsHonestNulls_NeverFabricatedCoordinates()
    {
        var (client, profileId) = await SetupAsync();
        var programId = await FindProgramIdAsync(client, "Russia", "Applied Mathematics");

        var response = await client.GetAsync($"/api/v1/profile/{profileId}/campus-map/{programId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var map = await response.Content.ReadFromJsonAsync<CampusMapDto>(TestJson.Options);
        map!.Coordinates.Should().BeNull();
        map.Environment.Should().BeNull();
        map.MapLink.Should().BeNull();
    }

    [Fact]
    public async Task Comparison_FoldsCampusMapDirectlyIntoEachRow_NotADisconnectedScreen()
    {
        var (client, profileId) = await SetupAsync();
        var kzProgramId = await FindProgramIdAsync(client, "Kazakhstan", "Computer Science");
        var ruProgramId = await FindProgramIdAsync(client, "Russia", "Applied Mathematics");

        var response = await client.PostAsJsonAsync("/api/v1/comparison", new CompareProgramsRequestDto(profileId, [kzProgramId, ruProgramId]), TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rows = await response.Content.ReadFromJsonAsync<List<ProgramComparisonRowDto>>(TestJson.Options);
        rows.Should().HaveCount(2);
        rows!.Should().OnlyContain(r => r.CampusMap != null);
        rows.Single(r => r.Program.ProgramId == kzProgramId).CampusMap.Coordinates.Should().NotBeNull();
        rows.Single(r => r.Program.ProgramId == ruProgramId).CampusMap.Coordinates.Should().BeNull();
    }
}
