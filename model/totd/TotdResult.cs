using PancakeBot.Api.model.player;

namespace PancakeBot.Api.model.totd;

public class TotdResult
{
    public int Id { get; set; }

    public required string MapUid { get; set; }
    public TotdMap? TotdMap { get; set; }

    public required string AccountId { get; set; }
    public Player? Player { get; set; }

    public int Rank { get; set; }
    public int Score { get; set; }
    public int PointsAwarded { get; set; }

    public long Timestamp { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
