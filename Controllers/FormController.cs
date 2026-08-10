namespace landingmvc.Controllers;

using Microsoft.AspNetCore.Mvc;
using landingmvc.Models;
using landingmvc.Services;

[ApiController]
[Route("[controller]")]
public class FormController : ControllerBase
{
    private readonly IEmailValidationService _emailValidationService;
    private readonly IGoogleSheetsService? _googleSheetsService;

    public FormController(
        IEmailValidationService emailValidationService,
        IGoogleSheetsService? googleSheetsService = null)
    {
        _emailValidationService = emailValidationService;
        _googleSheetsService = googleSheetsService;
    }

    [HttpPost("SubmitJoinBeta")]
    public async Task<IActionResult> SubmitJoinBeta([FromForm] JoinBetaRequest? request)
    {
        var resolvedRequest = await ResolveJoinBetaRequestAsync(request);
        var validationResult = await _emailValidationService.ValidateEmailAsync(resolvedRequest?.Email ?? string.Empty);

        if (!validationResult.IsValid)
        {
            return BadRequest(new FormResponse
            {
                Success = false,
                Message = validationResult.ErrorMessage,
                Errors = new[] { validationResult.ErrorMessage }
            });
        }

        if (_googleSheetsService != null)
        {
            try
            {
                await _googleSheetsService.AppendSubmissionAsync("waitinglist", resolvedRequest?.Email ?? string.Empty, null, null);
            }
            catch
            {
                // Graceful error isolation
            }
        }

        return Ok(new FormResponse
        {
            Success = true,
            Message = "✓ Request submitted! We will be in touch.",
            Errors = Array.Empty<string>()
        });
    }

    [HttpPost("ConnectTeam")]
    public async Task<IActionResult> ConnectTeam([FromForm] ConnectTeamRequest? request)
    {
        var resolvedRequest = await ResolveConnectTeamRequestAsync(request);
        var validationResult = await _emailValidationService.ValidateEmailAsync(resolvedRequest?.Email ?? string.Empty);

        if (!validationResult.IsValid)
        {
            return BadRequest(new FormResponse
            {
                Success = false,
                Message = validationResult.ErrorMessage,
                Errors = new[] { validationResult.ErrorMessage }
            });
        }

        if (_googleSheetsService != null)
        {
            try
            {
                await _googleSheetsService.AppendSubmissionAsync("connectwithteam", resolvedRequest?.Email ?? string.Empty, resolvedRequest?.Name, resolvedRequest?.Message);
            }
            catch
            {
                // Graceful error isolation
            }
        }

        return Ok(new FormResponse
        {
            Success = true,
            Message = "✓ Request submitted! We will be in touch.",
            Errors = Array.Empty<string>()
        });
    }

    private async Task<JoinBetaRequest> ResolveJoinBetaRequestAsync(JoinBetaRequest? request)
    {
        if (request != null && !string.IsNullOrWhiteSpace(request.Email))
        {
            return request;
        }

        var result = new JoinBetaRequest();
        if (Request.HasJsonContentType())
        {
            try
            {
                var body = await Request.ReadFromJsonAsync<JoinBetaRequest>();
                if (body != null && !string.IsNullOrWhiteSpace(body.Email))
                {
                    result.Email = body.Email;
                }
            }
            catch { }
        }
        else if (Request.HasFormContentType)
        {
            if (Request.Form.TryGetValue("email", out var emailVal))
            {
                result.Email = emailVal.ToString();
            }
        }

        return result;
    }

    private async Task<ConnectTeamRequest> ResolveConnectTeamRequestAsync(ConnectTeamRequest? request)
    {
        if (request != null && !string.IsNullOrWhiteSpace(request.Email))
        {
            return request;
        }

        var result = new ConnectTeamRequest();
        if (Request.HasJsonContentType())
        {
            try
            {
                var body = await Request.ReadFromJsonAsync<ConnectTeamRequest>();
                if (body != null)
                {
                    result.Email = body.Email ?? string.Empty;
                    result.Name = body.Name;
                    result.Message = body.Message;
                }
            }
            catch { }
        }
        else if (Request.HasFormContentType)
        {
            if (Request.Form.TryGetValue("email", out var emailVal))
            {
                result.Email = emailVal.ToString();
            }
            if (Request.Form.TryGetValue("name", out var nameVal))
            {
                result.Name = nameVal.ToString();
            }
            if (Request.Form.TryGetValue("message", out var msgVal))
            {
                result.Message = msgVal.ToString();
            }
        }

        return result;
    }
}
