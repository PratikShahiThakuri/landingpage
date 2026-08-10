namespace landingmvc.Models;

public class ConnectTeamRequest
{
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Message { get; set; }
}
