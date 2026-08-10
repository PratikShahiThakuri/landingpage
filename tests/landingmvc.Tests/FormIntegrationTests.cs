namespace landingmvc.Tests;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using landingmvc.Models;

public class FormIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public FormIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHomePage_ContainsBothFormsAndScripts()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("id=\"hero-form\"", html);
        Assert.Contains("id=\"final-form\"", html);
        Assert.Contains("id=\"hero-email\"", html);
        Assert.Contains("id=\"final-email\"", html);
        Assert.Contains("id=\"hero-feedback\"", html);
        Assert.Contains("id=\"final-feedback\"", html);
        Assert.Contains("id=\"hero-submit\"", html);
        Assert.Contains("id=\"final-submit\"", html);
        Assert.Contains("setupFormHandler('hero-form'", html);
        Assert.Contains("setupFormHandler('final-form'", html);
        Assert.Contains("/Form/SubmitJoinBeta", html);
        Assert.Contains("/Form/ConnectTeam", html);
    }

    [Fact]
    public async Task PostSubmitJoinBeta_ValidEmail_Returns200Json()
    {
        var client = _factory.CreateClient();
        var request = new { email = "test.beta@gmail.com" };

        var response = await client.PostAsJsonAsync("/Form/SubmitJoinBeta", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<FormResponse>();
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.Contains("submitted", body.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(body.Errors);
    }

    [Fact]
    public async Task PostSubmitJoinBeta_InvalidEmail_Returns400Json()
    {
        var client = _factory.CreateClient();
        var request = new { email = "not-an-email" };

        var response = await client.PostAsJsonAsync("/Form/SubmitJoinBeta", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<FormResponse>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.NotEmpty(body.Errors);
        Assert.Equal("Please enter a valid email address.", body.Message);
    }

    [Fact]
    public async Task PostSubmitJoinBeta_DisposableEmail_Returns400Json()
    {
        var client = _factory.CreateClient();
        var request = new { email = "user@yopmail.com" };

        var response = await client.PostAsJsonAsync("/Form/SubmitJoinBeta", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<FormResponse>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.NotEmpty(body.Errors);
        Assert.Contains("temporary addresses are not accepted", body.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostConnectTeam_ValidEmail_Returns200Json()
    {
        var client = _factory.CreateClient();
        var request = new { email = "client@microsoft.com", name = "Alice", message = "Demo request" };

        var response = await client.PostAsJsonAsync("/Form/ConnectTeam", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<FormResponse>();
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.Contains("submitted", body.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(body.Errors);
    }

    [Fact]
    public async Task PostConnectTeam_InvalidEmail_Returns400Json()
    {
        var client = _factory.CreateClient();
        var request = new { email = "invalid.domain.without.at" };

        var response = await client.PostAsJsonAsync("/Form/ConnectTeam", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<FormResponse>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.NotEmpty(body.Errors);
    }

    [Fact]
    public async Task PostFormUrlEncoded_ValidEmail_Returns200()
    {
        var client = _factory.CreateClient();
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("email", "formencoded@gmail.com")
        });

        var response = await client.PostAsync("/Form/SubmitJoinBeta", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<FormResponse>();
        Assert.NotNull(body);
        Assert.True(body.Success);
    }
}
