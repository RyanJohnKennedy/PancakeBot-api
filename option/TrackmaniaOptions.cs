namespace PancakeBot.Api.Option;

public class TrackmaniaOptions
{
    public const string SectionName = "Trackmania";

    // Nadeo basic-auth credentials used to obtain service tokens. Configure these
    // through user secrets or environment variables, not source-controlled JSON.
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public string CoreUrl { get; set; } = string.Empty;

    public string LiveUrl { get; set; } = string.Empty;
    public string OAuthUrl { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // OAuth client-credentials settings for Trackmania's public OAuth API.
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    // Documents the currently supported Nadeo authentication flow and leaves
    // room for future token strategies without changing endpoint consumers.
    public string NadeoTokenStrategy { get; set; } = "BasicCredentials";

    public string DefaultRegion { get; set; } = "SouthAfrica";
    public Dictionary<string, string> Regions { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SouthAfrica"] = "3022580b-7e13-11e8-8060-e284abfd2bc4"
    };

    public int DailyLeaderboardSize { get; set; } = 5;
    public Dictionary<int, int> PointsByPosition { get; set; } = new()
    {
        [1] = 5,
        [2] = 4,
        [3] = 3,
        [4] = 2,
        [5] = 1
    };

    public string GetDefaultRegionZoneId()
    {
        if (Regions.TryGetValue(DefaultRegion, out var zoneId) && !string.IsNullOrWhiteSpace(zoneId))
            return zoneId;

        throw new InvalidOperationException(
            $"Trackmania default region '{DefaultRegion}' is not configured in Trackmania:Regions.");
    }
}
