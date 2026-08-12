namespace PancakeBot.Api.model.totd;

public sealed record TotdMonthLeaderboardResponse(
    DateOnly Month,
    IReadOnlyList<TotdMonthLeaderboardResult> Results);

public sealed record TotdMonthLeaderboardResult(
    int Rank,
    string PlayerName,
    int PointsAwarded,
    int FirstPlaceCount,
    int SecondPlaceCount,
    int ThirdPlaceCount,
    int FourthPlaceCount,
    int FifthPlaceCount);
