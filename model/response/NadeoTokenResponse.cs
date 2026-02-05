namespace PancakeBot.Api.Model.Response;

public class NadeoTokenResponse
{
    public required string AccessToken { get; set; }
    public int ExpiresIn { get; set; }
}
