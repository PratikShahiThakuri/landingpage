namespace landingmvc.Services;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class GoogleSheetsService : IGoogleSheetsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleSheetsService> _logger;
    private static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };

    public GoogleSheetsService(IConfiguration configuration, ILogger<GoogleSheetsService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> AppendSubmissionAsync(string formType, string email, string? name, string? message)
    {
        string? spreadsheetId = _configuration["GoogleSheets:SpreadsheetId"]
            ?? Environment.GetEnvironmentVariable("GOOGLE_SHEETS_SPREADSHEET_ID");

        string? jsonCredentialsPath = _configuration["GoogleSheets:CredentialsPath"]
            ?? Environment.GetEnvironmentVariable("GOOGLE_SHEETS_CREDENTIALS_PATH");

        if (string.IsNullOrWhiteSpace(spreadsheetId) || string.IsNullOrWhiteSpace(jsonCredentialsPath))
        {
            _logger.LogWarning("Google Sheets configuration or Service Account path is missing.");
            return false;
        }

        try
        {
            GoogleCredential googleCredential;

            await using (var stream = File.OpenRead(jsonCredentialsPath))
            {
                var serviceAccount = await CredentialFactory.FromStreamAsync<ServiceAccountCredential>(stream, CancellationToken.None);

                googleCredential = GoogleCredential.FromServiceAccountCredential(serviceAccount)
                    .CreateScoped(Scopes);
            }

            var service = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = googleCredential,
                ApplicationName = "LandingMVC"
            });

            string tabName = MapTabName(formType);
            string timestampUtc = DateTime.UtcNow.ToString("o");

            var valueRange = new ValueRange
            {
                Values = new List<IList<object>>
                {
                    new List<object>
                    {
                        timestampUtc,
                        email ?? string.Empty,
                        name ?? string.Empty,
                        message ?? string.Empty
                    }
                }
            };

            var request = service.Spreadsheets.Values.Append(valueRange, spreadsheetId, $"{tabName}!A1");
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

            var response = await request.ExecuteAsync();

            _logger.LogInformation("Successfully appended submission row to Google Sheet tab '{TabName}'.", tabName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append row to Google Sheet.");
            return false;
        }
    }

    private static string MapTabName(string formType)
    {
        if (string.Equals(formType, "Join Beta", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(formType, "waitinglist", StringComparison.OrdinalIgnoreCase))
        {
            return "waitinglist";
        }

        if (string.Equals(formType, "Connect with Team", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(formType, "connectwithteam", StringComparison.OrdinalIgnoreCase))
        {
            return "connectwithteam";
        }

        return string.IsNullOrWhiteSpace(formType) ? "waitinglist" : formType;
    }
}