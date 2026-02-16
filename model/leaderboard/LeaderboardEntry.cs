namespace PancakeBot.Api.model.Leaderboard;

public class LeaderboardEntry
{
    public string AccountId { get; set; } = string.Empty;

    public string ZoneId { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;

    public int Position { get; set; }
    public int Score { get; set; }

    public long Timestamp { get; set; }
}