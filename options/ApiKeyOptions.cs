namespace PancakeBot.Api.Options;

public sealed class ApiKeyOptions
{
    public const string SectionName = "ApiKey";

    public string Key { get; init; } = null!;
}