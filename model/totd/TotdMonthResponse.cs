namespace PancakeBot.Api.model.totd;

public class TotdMonthResponse
{
    public List<TotdMonth> MonthList { get; set; } = [];
    public int ItemCount { get; set; }
    public long NextRequestTimestamp { get; set; }
    public int RelativeNextRequest { get; set; }
}