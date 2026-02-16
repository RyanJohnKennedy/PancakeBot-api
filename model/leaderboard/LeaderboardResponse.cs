namespace PancakeBot.Api.model.Leaderboard;

public class LeaderboardResponse
{
    public string GroupUid { get; set; } = string.Empty;
    public string MapUid { get; set; } = string.Empty;

    public List<LeaderboardTopZone> Tops { get; set; } = [];
}