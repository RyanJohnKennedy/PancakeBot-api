namespace PancakeBot.Api.model.totd;

public class TotdMap
{
    public required string MapUid { get; set; }
    public string? MapName { get; set; }

    public DateOnly TotdDate { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int CampaignId { get; set; }
    public string? CampaignUid { get; set; }
    public string? SeasonUid { get; set; }

    public long StartTimestamp { get; set; }
    public long EndTimestamp { get; set; }
}
