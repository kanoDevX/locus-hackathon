using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using UstazAI.Application.Auth;
using UstazAI.Endpoints;

namespace UstazAI.Tests;

public sealed class AccountIntegrationTests(UstazApiFactory factory) : IClassFixture<UstazApiFactory>
{
    private async Task<(HttpClient Client, string Email)> RegisterAsync()
    {
        var client = factory.CreateClient();
        var email = $"account-{Guid.NewGuid():N}@test.ustazai";
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, "TestPassword123!", "Account Test Student"));
        var auth = await register.Content.ReadFromJsonAsync<AuthResultDto>(TestJson.Options);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return (client, email);
    }

    [Fact]
    public async Task UpdateAccount_ChangesDisplayNameAndEmail()
    {
        var (client, _) = await RegisterAsync();
        var newEmail = $"account-updated-{Guid.NewGuid():N}@test.ustazai";

        var response = await client.PutAsJsonAsync("/api/v1/auth/account", new UpdateAccountRequestDto("New Name", newEmail));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var account = await response.Content.ReadFromJsonAsync<AccountDto>(TestJson.Options);
        account!.DisplayName.Should().Be("New Name");
        account.Email.Should().Be(newEmail);
    }

    [Fact]
    public async Task UpdateAccount_RejectsEmailAlreadyUsedByAnotherAccount()
    {
        var (clientA, emailA) = await RegisterAsync();
        var (clientB, _) = await RegisterAsync();

        var response = await clientB.PutAsJsonAsync("/api/v1/auth/account", new UpdateAccountRequestDto("Someone Else", emailA));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ReturnsUnauthorized()
    {
        var (client, _) = await RegisterAsync();

        var response = await client.PostAsJsonAsync("/api/v1/auth/change-password", new ChangePasswordRequestDto("WrongPassword1!", "NewPassword123!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WithCorrectCurrentPassword_SucceedsAndOldSessionsAreRevoked()
    {
        var (client, email) = await RegisterAsync();

        var changeResponse = await client.PostAsJsonAsync("/api/v1/auth/change-password", new ChangePasswordRequestDto("TestPassword123!", "NewPassword123!"));
        changeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var oldLoginAttempt = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, "TestPassword123!"));
        oldLoginAttempt.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var newLoginAttempt = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, "NewPassword123!"));
        newLoginAttempt.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
