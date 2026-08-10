namespace landingmvc.Services;

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using landingmvc.Models;

public class EmailValidationService : IEmailValidationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmailValidationService> _logger;

    private static readonly Regex RfcRegex = new Regex(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly HashSet<string> DisposableDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "yopmail.com", "mailinator.com", "guerrillamail.com", "trashmail.com",
        "10minutemail.com", "throwam.com", "sharklasers.com", "maildrop.cc",
        "fakeinbox.com", "spamgourmet.com", "dispostable.com", "tempinbox.com",
        "getairmail.com", "tempmail.com", "throwawaymail.com", "getnada.com",
        "mohmal.com", "disposablemail.com", "crazymailing.com", "tmail.ws",
        "boun.cr", "inboxkitten.com", "mailnesia.com", "mytemp.email",
        "emailondeck.com", "tempail.com", "grr.la", "guerrillamailblock.com",
        "pokemail.net", "spam4.me", "superrito.com", "armyspy.com",
        "cuvox.de", "dayrep.com", "einrot.com", "fleckens.hu",
        "gustr.com", "jourrapide.com", "rhyta.com", "teleworm.us",
        "10minutemail.net", "temp-mail.org", "guerrillamail.net", "guerrillamail.org"
    };

    public EmailValidationService(HttpClient httpClient, ILogger<EmailValidationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ValidationResult> ValidateEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return ValidationResult.Failure("Please enter a valid email address.");
        }

        string trimmedEmail = email.Trim();

        // Stage 1: RFC 5322 Validation
        if (!RfcRegex.IsMatch(trimmedEmail))
        {
            return ValidationResult.Failure("Please enter a valid email address.");
        }

        // Stage 2: Disposable Domain Check (Exact Match & Subdomain Suffix Matching)
        string[] parts = trimmedEmail.Split('@');
        if (parts.Length != 2)
        {
            return ValidationResult.Failure("Please enter a valid email address.");
        }

        string domain = parts[1].ToLowerInvariant();
        bool isDisposable = DisposableDomains.Contains(domain) ||
                            DisposableDomains.Any(d => domain.EndsWith("." + d, StringComparison.OrdinalIgnoreCase));

        if (isDisposable)
        {
            return ValidationResult.Failure("Please use your real work or personal email — temporary addresses are not accepted.");
        }

        // Stage 3: Async MX Lookup via api.mailcheck.ai with 3s Timeout & Graceful Fallback
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            string url = $"https://api.mailcheck.ai/email/{Uri.EscapeDataString(trimmedEmail)}";

            var response = await _httpClient.GetAsync(url, cts.Token);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MailcheckResponse>(cancellationToken: cts.Token);
                if (result != null)
                {
                    if (result.Disposable == true)
                    {
                        return ValidationResult.Failure("Please use your real work or personal email — temporary addresses are not accepted.");
                    }
                    if (result.Mx == false)
                    {
                        return ValidationResult.Failure("Domain does not appear to accept email. Please check for typos.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Mailcheck API validation check failed or timed out. Falling back gracefully.");
        }

        return ValidationResult.Success();
    }

    private class MailcheckResponse
    {
        [JsonPropertyName("disposable")]
        public bool? Disposable { get; set; }

        [JsonPropertyName("mx")]
        public bool? Mx { get; set; }
    }
}
