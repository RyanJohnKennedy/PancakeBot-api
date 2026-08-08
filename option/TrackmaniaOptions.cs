namespace PancakeBot.Api.Option;

public class TrackmaniaOptions
{
    public required string Login { get; set; }
    public required string Password { get; set; }

    public required string CoreUrl { get; set; }

    public required string LiveUrl { get; set; }
    public required string OAuthUrl { get; set; }
    public required string UserAgent { get; set; }
    public required string Email { get; set; }
    public required string SouthAfricaZoneId { get; set; }
}
