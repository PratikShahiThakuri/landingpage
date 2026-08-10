namespace landingmvc.Services;

using landingmvc.Models;

public interface IEmailValidationService
{
    Task<ValidationResult> ValidateEmailAsync(string email);
}
