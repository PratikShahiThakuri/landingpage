namespace landingmvc.Models;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public static ValidationResult Success() => new ValidationResult { IsValid = true };
    public static ValidationResult Failure(string message) => new ValidationResult { IsValid = false, ErrorMessage = message };
}
