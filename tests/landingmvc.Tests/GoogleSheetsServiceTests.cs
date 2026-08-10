namespace landingmvc.Tests;

using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using landingmvc.Services;
using landingmvc.Controllers;
using landingmvc.Models;
using System.Text.Json;
using System.Text;
using System.Net;

public class GoogleSheetsServiceTests
{
    private class AsyncFakeHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, Task<HttpResponseMessage>> Handler { get; set; }

        public AsyncFakeHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            Handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Handler(request);
        }
    }

    private static IConfiguration CreateConfig(string spreadsheetId = "test-sheet-id", string apiKey = "test-api-key")
    {
        var settings = new Dictionary<string, string?>
        {
            { "GoogleSheets:SpreadsheetId", spreadsheetId },
            { "GoogleSheets:ApiKey", apiKey }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    [Fact]
    public async Task AppendSubmissionAsync_WaitingList_SendsCorrectUrlAndPayload()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedRequestBody = null;

        var handler = new AsyncFakeHttpMessageHandler(async req =>
        {
            capturedRequest = req;
            if (req.Content != null)
            {
                capturedRequestBody = await req.Content.ReadAsStringAsync();
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"updates\":{\"updatedRows\":1}}", Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler);
        var config = CreateConfig();
        var logger = NullLogger<GoogleSheetsService>.Instance;
        var service = new GoogleSheetsService(httpClient, config, logger);

        bool result = await service.AppendSubmissionAsync("waitinglist", "user@example.com", null, null);

        Assert.True(result);
        Assert.NotNull(capturedRequest);
        Assert.Contains("https://sheets.googleapis.com/v4/spreadsheets/test-sheet-id/values/waitinglist:append", capturedRequest!.RequestUri!.ToString());
        Assert.Contains("key=test-api-key", capturedRequest.RequestUri.ToString());
        Assert.NotNull(capturedRequestBody);
        Assert.Contains("\"range\":\"waitinglist\"", capturedRequestBody!);
        Assert.Contains("user@example.com", capturedRequestBody!);
    }

    [Fact]
    public async Task AppendSubmissionAsync_ConnectWithTeam_SendsCorrectUrlAndPayload()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedRequestBody = null;

        var handler = new AsyncFakeHttpMessageHandler(async req =>
        {
            capturedRequest = req;
            if (req.Content != null)
            {
                capturedRequestBody = await req.Content.ReadAsStringAsync();
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"updates\":{\"updatedRows\":1}}", Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler);
        var config = CreateConfig();
        var logger = NullLogger<GoogleSheetsService>.Instance;
        var service = new GoogleSheetsService(httpClient, config, logger);

        bool result = await service.AppendSubmissionAsync("connectwithteam", "contact@company.com", "Alice Smith", "Inquiry about pricing");

        Assert.True(result);
        Assert.NotNull(capturedRequest);
        Assert.Contains("https://sheets.googleapis.com/v4/spreadsheets/test-sheet-id/values/connectwithteam:append", capturedRequest!.RequestUri!.ToString());
        Assert.NotNull(capturedRequestBody);
        Assert.Contains("\"range\":\"connectwithteam\"", capturedRequestBody!);
        Assert.Contains("contact@company.com", capturedRequestBody!);
        Assert.Contains("Alice Smith", capturedRequestBody!);
        Assert.Contains("Inquiry about pricing", capturedRequestBody!);
    }

    [Fact]
    public async Task AppendSubmissionAsync_JoinBetaTabMapping_MapsToWaitingList()
    {
        HttpRequestMessage? capturedRequest = null;

        var handler = new AsyncFakeHttpMessageHandler(req =>
        {
            capturedRequest = req;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var httpClient = new HttpClient(handler);
        var config = CreateConfig();
        var logger = NullLogger<GoogleSheetsService>.Instance;
        var service = new GoogleSheetsService(httpClient, config, logger);

        bool result = await service.AppendSubmissionAsync("Join Beta", "user@example.com", null, null);

        Assert.True(result);
        Assert.NotNull(capturedRequest);
        Assert.Contains("/values/waitinglist:append", capturedRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task AppendSubmissionAsync_MissingApiKey_ReturnsFalseWithoutCallingHttp()
    {
        bool httpCalled = false;
        var handler = new AsyncFakeHttpMessageHandler(req =>
        {
            httpCalled = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var httpClient = new HttpClient(handler);
        var config = CreateConfig(spreadsheetId: "test-sheet", apiKey: ""); // Empty API key
        var logger = NullLogger<GoogleSheetsService>.Instance;
        var service = new GoogleSheetsService(httpClient, config, logger);

        bool result = await service.AppendSubmissionAsync("waitinglist", "user@example.com", null, null);

        Assert.False(result);
        Assert.False(httpCalled);
    }

    [Fact]
    public async Task AppendSubmissionAsync_HttpErrorResponse_ReturnsFalseGracefully()
    {
        var handler = new AsyncFakeHttpMessageHandler(req =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{\"error\": {\"code\": 500, \"message\": \"Internal Error\"}}")
            });
        });

        var httpClient = new HttpClient(handler);
        var config = CreateConfig();
        var logger = NullLogger<GoogleSheetsService>.Instance;
        var service = new GoogleSheetsService(httpClient, config, logger);

        bool result = await service.AppendSubmissionAsync("waitinglist", "user@example.com", null, null);

        Assert.False(result);
    }

    [Fact]
    public async Task AppendSubmissionAsync_HttpRequestException_ReturnsFalseGracefully()
    {
        var handler = new AsyncFakeHttpMessageHandler(req =>
        {
            throw new HttpRequestException("Network failure");
        });

        var httpClient = new HttpClient(handler);
        var config = CreateConfig();
        var logger = NullLogger<GoogleSheetsService>.Instance;
        var service = new GoogleSheetsService(httpClient, config, logger);

        bool result = await service.AppendSubmissionAsync("waitinglist", "user@example.com", null, null);

        Assert.False(result);
    }

    [Fact]
    public async Task FormController_InvokesGoogleSheetsServiceOnSuccess()
    {
        var emailValidationService = new FakeEmailValidationService(true);
        var mockSheetsService = new FakeGoogleSheetsService();

        var controller = new FormController(emailValidationService, mockSheetsService);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var joinResult = await controller.SubmitJoinBeta(new JoinBetaRequest { Email = "valid@example.com" });
        Assert.IsType<OkObjectResult>(joinResult);
        Assert.True(mockSheetsService.AppendSubmissionCalled);
        Assert.Equal("waitinglist", mockSheetsService.LastFormType);
        Assert.Equal("valid@example.com", mockSheetsService.LastEmail);

        mockSheetsService.Reset();

        var connectResult = await controller.ConnectTeam(new ConnectTeamRequest
        {
            Email = "team@example.com",
            Name = "Bob",
            Message = "Hello"
        });
        Assert.IsType<OkObjectResult>(connectResult);
        Assert.True(mockSheetsService.AppendSubmissionCalled);
        Assert.Equal("connectwithteam", mockSheetsService.LastFormType);
        Assert.Equal("team@example.com", mockSheetsService.LastEmail);
        Assert.Equal("Bob", mockSheetsService.LastName);
        Assert.Equal("Hello", mockSheetsService.LastMessage);
    }

    private class FakeEmailValidationService : IEmailValidationService
    {
        private readonly bool _isValid;

        public FakeEmailValidationService(bool isValid)
        {
            _isValid = isValid;
        }

        public Task<ValidationResult> ValidateEmailAsync(string email)
        {
            return Task.FromResult(new ValidationResult { IsValid = _isValid, ErrorMessage = _isValid ? string.Empty : "Invalid email" });
        }
    }

    private class FakeGoogleSheetsService : IGoogleSheetsService
    {
        public bool AppendSubmissionCalled { get; private set; }
        public string? LastFormType { get; private set; }
        public string? LastEmail { get; private set; }
        public string? LastName { get; private set; }
        public string? LastMessage { get; private set; }

        public Task<bool> AppendSubmissionAsync(string formType, string email, string? name, string? message)
        {
            AppendSubmissionCalled = true;
            LastFormType = formType;
            LastEmail = email;
            LastName = name;
            LastMessage = message;
            return Task.FromResult(true);
        }

        public void Reset()
        {
            AppendSubmissionCalled = false;
            LastFormType = null;
            LastEmail = null;
            LastName = null;
            LastMessage = null;
        }
    }
}
