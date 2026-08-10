namespace landingmvc.Tests;

using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using landingmvc.Controllers;
using landingmvc.Models;
using landingmvc.Services;
using System.Text.Json;
using System.Text;
using System.Net;

public class FormControllerTests
{
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    private EmailValidationService CreateValidationService(Func<HttpRequestMessage, HttpResponseMessage>? mockHttp = null)
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            if (mockHttp != null) return mockHttp(req);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"disposable\":false,\"mx\":true}", Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler);
        var logger = NullLogger<EmailValidationService>.Instance;
        return new EmailValidationService(httpClient, logger);
    }

    private FormController CreateController(IEmailValidationService validationService)
    {
        var controller = new FormController(validationService);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    [Theory]
    [InlineData("valid.user@example.com")]
    [InlineData("test.account@domain.co.uk")]
    [InlineData("corporate_user@techfirm.org")]
    public async Task SubmitJoinBeta_ValidEmail_ReturnsOk(string email)
    {
        var validationService = CreateValidationService();
        var controller = CreateController(validationService);

        var request = new JoinBetaRequest { Email = email };
        var result = await controller.SubmitJoinBeta(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<FormResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Contains("submitted", response.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(response.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("plainaddress")]
    [InlineData("#@%^%#$@#$@#.com")]
    [InlineData("@example.com")]
    [InlineData("Joe Smith <email@example.com>")]
    [InlineData("email.example.com")]
    public async Task SubmitJoinBeta_InvalidEmail_ReturnsBadRequest(string email)
    {
        var validationService = CreateValidationService();
        var controller = CreateController(validationService);

        var request = new JoinBetaRequest { Email = email };
        var result = await controller.SubmitJoinBeta(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<FormResponse>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.NotEmpty(response.Errors);
        Assert.Equal("Please enter a valid email address.", response.Message);
    }

    [Theory]
    [InlineData("user@yopmail.com")]
    [InlineData("test@mailinator.com")]
    [InlineData("spam@guerrillamail.com")]
    [InlineData("user@sub.yopmail.com")]
    public async Task SubmitJoinBeta_DisposableEmail_ReturnsBadRequest(string email)
    {
        var validationService = CreateValidationService();
        var controller = CreateController(validationService);

        var request = new JoinBetaRequest { Email = email };
        var result = await controller.SubmitJoinBeta(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<FormResponse>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.NotEmpty(response.Errors);
        Assert.Contains("temporary addresses are not accepted", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitJoinBeta_JsonBodyFallback_ResolvesEmail()
    {
        var validationService = CreateValidationService();
        var controller = new FormController(validationService);

        var json = JsonSerializer.Serialize(new { email = "json.user@example.com" });
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "application/json";
        httpContext.Request.Body = stream;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.SubmitJoinBeta(null);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<FormResponse>(okResult.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task ConnectTeam_ValidEmailAndDetails_ReturnsOk()
    {
        var validationService = CreateValidationService();
        var controller = CreateController(validationService);

        var request = new ConnectTeamRequest
        {
            Email = "lead@enterprise.com",
            Name = "Jane Doe",
            Message = "Interested in RBAC architecture."
        };
        var result = await controller.ConnectTeam(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<FormResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Contains("submitted", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConnectTeam_InvalidEmail_ReturnsBadRequest()
    {
        var validationService = CreateValidationService();
        var controller = CreateController(validationService);

        var request = new ConnectTeamRequest
        {
            Email = "invalid-email-address",
            Name = "John Doe"
        };
        var result = await controller.ConnectTeam(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<FormResponse>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.NotEmpty(response.Errors);
    }

    [Fact]
    public async Task ValidationService_ExternalApiTimeout_FallsBackGracefully()
    {
        var validationService = CreateValidationService(req =>
        {
            throw new TaskCanceledException("Simulated timeout");
        });

        var result = await validationService.ValidateEmailAsync("user@example.com");

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidationService_ExternalApiDisposable_RejectsEmail()
    {
        var validationService = CreateValidationService(req =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"disposable\":true,\"mx\":true}", Encoding.UTF8, "application/json")
            };
        });

        var result = await validationService.ValidateEmailAsync("customdisposable@unknowndomain.com");

        Assert.False(result.IsValid);
        Assert.Contains("temporary addresses are not accepted", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidationService_ExternalApiNoMx_RejectsEmail()
    {
        var validationService = CreateValidationService(req =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"disposable\":false,\"mx\":false}", Encoding.UTF8, "application/json")
            };
        });

        var result = await validationService.ValidateEmailAsync("user@nomxdomain.com");

        Assert.False(result.IsValid);
        Assert.Contains("does not appear to accept email", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
