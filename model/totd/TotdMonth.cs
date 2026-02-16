namespace PancakeBot.Api.model.totd;

public class TotdMonth
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int LastDay { get; set; }

    public List<TotdDay> Days { get; set; } = [];
}
