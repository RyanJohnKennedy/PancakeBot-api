namespace PancakeBot.Api.provider;

public class TrackmaniaOAuthTokenProvider
{
    private readonly SemaphoreSlim _lock = new(1,1);

    private string? _token;
    private DateTime _expires;

    public async Task<string> GetToken(Func<Task<(string token,int expires)>> factory)
    {
        if (_token != null && DateTime.UtcNow < _expires)
            return _token;

        await _lock.WaitAsync();
        try
        {
            if (_token != null && DateTime.UtcNow < _expires)
                return _token;

            var result = await factory();

            _token = result.token;
            _expires = DateTime.UtcNow.AddSeconds(result.expires - 60);

            return _token;
        }
        finally
        {
            _lock.Release();
        }
    }
}
