namespace landingmvc.Services;

public interface IGoogleSheetsService
{
    Task<bool> AppendSubmissionAsync(string formType, string email, string? name, string? message);
}
