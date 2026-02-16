namespace PancakeBot.Api.model.Leaderboard;

public class LeaderboardTopZone
{
    public string ZoneId { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;

    public List<LeaderboardEntry> Top { get; set; } = [];
}