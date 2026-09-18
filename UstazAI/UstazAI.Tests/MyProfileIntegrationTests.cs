using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using UstazAI.Application.Auth;
using UstazAI.Application.Dtos;
using UstazAI.Application.Profiles;
using UstazAI.Domain.Enums;
using UstazAI.Endpoints;

namespace UstazAI.Tests;

public sealed class MyProfileIntegrationTests(UstazApiFactory factory) : IClassFixture<UstazApiFactory>
{
    private static async Task<HttpClient> RegisterAsync(UstazApiFactory factory, string email)
    {
        var client = factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, "TestPassword123!", "My Profile Test Student"));
        var auth = await register.Content.ReadFromJsonAsync<AuthResultDto>(TestJson.Options);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    [Fact]
    public async Task NoProfileYet_ReturnsOkWithEmptyBody_NeverA404()
    {
        var client = await RegisterAsync(factory, $"mine-empty-{Guid.NewGuid():N}@test.ustazai");

        var response = await client.GetAsync("/api/v1/profile/mine");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().BeEmpty("Results.Ok(null) writes no content — the frontend's apiFetch treats an empty 200 body as \"no value\" rather than parsing it as JSON");
    }

    [Fact]
    public async Task AfterCreatingAProfile_MineResolvesItById_WithoutTheClientAlreadyKnowingTheId()
    {
        var client = await RegisterAsync(factory, $"mine-{Guid.NewGuid():N}@test.ustazai");
        var profileRequest = new ProfileRequestDto(
            ProfileId: null, FullName: "My Profile Test Student", Grade: 11, Age: 17, PreferredLanguage: Locale.En,
            Interests: ["Computer Science"], Gpa: 3.6m, ExamScores: [], TargetCountries: ["Kazakhstan"],
            BudgetBand: BudgetBand.Medium, TimelineMonthsToApplication: 10, Constraints: []);
        var createResponse = await client.PostAsJsonAsync("/api/v1/profile", profileRequest, TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateOrUpdateProfileResult>(TestJson.Options);

        var mineResponse = await client.GetAsync("/api/v1/profile/mine");

        mineResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var mine = await mineResponse.Content.ReadFromJsonAsync<ProfileDto>(TestJson.Options);
        mine!.Id.Should().Be(created!.Profile.Id);
    }

    [Fact]
    public async Task TwoDifferentAccounts_EachOnlySeeTheirOwnProfileViaMine()
    {
        var clientA = await RegisterAsync(factory, $"mine-a-{Guid.NewGuid():N}@test.ustazai");
        var clientB = await RegisterAsync(factory, $"mine-b-{Guid.NewGuid():N}@test.ustazai");

        var requestA = new ProfileRequestDto(
            ProfileId: null, FullName: "Student A", Grade: 11, Age: 17, PreferredLanguage: Locale.En,
            Interests: [], Gpa: null, ExamScores: [], TargetCountries: [], BudgetBand: BudgetBand.Medium,
            TimelineMonthsToApplication: 10, Constraints: []);
        var createA = await clientA.PostAsJsonAsync("/api/v1/profile", requestA, TestJson.Options);
        var profileA = await createA.Content.ReadFromJsonAsync<CreateOrUpdateProfileResult>(TestJson.Options);

        var mineB = await clientB.GetAsync("/api/v1/profile/mine");
        var bodyB = await mineB.Content.ReadAsStringAsync();

        bodyB.Should().BeEmpty("account B has no profile of its own and must never see account A's");
        profileA!.Profile.FullName.Should().Be("Student A");
    }
}
