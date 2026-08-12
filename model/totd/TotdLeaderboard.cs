namespace PancakeBot.Api.model.totd;

public sealed record TotdLeaderboardResponse(
    string MapUid,
    string? MapName,
    DateOnly TotdDate,
    IReadOnlyList<TotdLeaderboardResult> Results);

public sealed record TotdLeaderboardResult(
    int Rank,
    string PlayerName,
    int Score,
    int PointsAwarded);
