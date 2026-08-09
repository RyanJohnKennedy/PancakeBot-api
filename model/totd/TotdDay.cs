namespace PancakeBot.Api.model.totd;

public class TotdDay
{
    public int CampaignId { get; set; }
    public string MapUid { get; set; } = string.Empty;
    public string? MapName { get; set; }

    public int Day { get; set; }
    public int MonthDay { get; set; }

    public string SeasonUid { get; set; } = string.Empty;
    public string? CampaignUid { get; set; }
    public string? LeaderboardGroup { get; set; }

    public long StartTimestamp { get; set; }
    public long EndTimestamp { get; set; }

    public int RelativeStart { get; set; }
    public int RelativeEnd { get; set; }
}
