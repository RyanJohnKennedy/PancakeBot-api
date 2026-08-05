namespace PancakeBot.Api.model.player;

public class Player
{
    public required string AccountId { get; set; }
    public required string DisplayName { get; set; }

    public string? ZoneId { get; set; }
    public string? ZoneName { get; set; }
    public string? CountryCode { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
