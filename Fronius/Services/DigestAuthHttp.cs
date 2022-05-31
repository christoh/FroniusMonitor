namespace De.Hochstaetter.Fronius.Services;

// Algorithm must be MD5 (MD5-sess and SHA is not supported)
// qop must be auth (auth-int and none is not supported)

public class DigestAuthHttp : IDisposable, IAsyncDisposable
{
    private static readonly Random random = new(unchecked((int)DateTime.UtcNow.Ticks));
    private readonly MD5 md5 = MD5.Create();
    private readonly HttpClient httpClient = new();
    private readonly WebConnection connection;
    private string? ha1;
    private string? realm;
    private string? nonce;
    private string? qop;
    private string? cnonce;
    private DateTime cnonceDate;
    private uint nc;

    public DigestAuthHttp(WebConnection connection)
    {
        this.connection = connection;
        httpClient.BaseAddress = new Uri(connection.BaseUrl);
    }

    public void Dispose()
    {
        md5.Dispose();
        httpClient.Dispose();
    }

    public async ValueTask DisposeAsync() => await Task.Run(Dispose).ConfigureAwait(false);

    public async Task<string> GetString(string url, JToken? token = null)
    {
        url = '/' + url;

        var request = await CreateRequest(url, token).ConfigureAwait(false);
        HttpResponseMessage? response = null;

        try
        {
            response = await httpClient.SendAsync(request).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                request.Dispose();
                var wwwAuthenticateHeader = response.Headers.GetValues("X-WWW-Authenticate").Single() ?? throw new UnauthorizedAccessException("No X-WWW-Authenticate header");
                response.Dispose();
                realm = await GetAuthHeaderToken("realm", wwwAuthenticateHeader).ConfigureAwait(false);
                nonce = await GetAuthHeaderToken("nonce", wwwAuthenticateHeader).ConfigureAwait(false);
                qop = await GetAuthHeaderToken("qop", wwwAuthenticateHeader).ConfigureAwait(false);
                nc = 0;
                cnonce = unchecked((uint)random.Next(int.MinValue, int.MaxValue)).ToString("x8");
                cnonceDate = DateTime.UtcNow;
                request = await CreateRequest(url, token).ConfigureAwait(false);
                response = await httpClient.SendAsync(request).ConfigureAwait(false);
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        finally
        {
            response?.Dispose();
            request.Dispose();
        }
    }

    private async Task<HttpRequestMessage> CreateRequest(string url, JToken? token)
    {
        var request = new HttpRequestMessage(token != null ? HttpMethod.Post : HttpMethod.Get, url);


        if (token != null)
        {
            await Task.Run(() => request.Content = new StringContent(token.ToString(), Encoding.UTF8, "application/json")).ConfigureAwait(false);
        }

        if (!string.IsNullOrEmpty(cnonce) && (DateTime.UtcNow - cnonceDate).TotalHours < 1.0)
        {
            request.Headers.Add("Authorization", await CreateDigestHeader(url, token != null).ConfigureAwait(false));
        }

        return request;
    }

    private Task<string> CalculateMd5Hash(string input) => Task.Run(() =>
    {
        var hash = md5.ComputeHash(Encoding.ASCII.GetBytes(input));

        return hash
            .Aggregate
            (
                new StringBuilder(hash.Length << 1),
                (stringBuilder, hashByte) => stringBuilder.Append(hashByte.ToString("x2"))
            )
            .ToString();
    });

    private static Task<string> GetAuthHeaderToken(string varName, string header) => Task.Run(() =>
    {
        var regex = new Regex($@"{varName}=""([^""]*)""");
        var matchHeader = regex.Match(header);

        if (matchHeader.Success)
        {
            return matchHeader.Groups[1].Value;
        }

        throw new InvalidOperationException($"Header {varName} not found");
    });

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private async ValueTask<string> CreateDigestHeader(string url, bool isPost)
    {
        ha1 ??= await CalculateMd5Hash($"{connection.UserName}:{realm}:{connection.Password}").ConfigureAwait(false);
        var ha2 = await CalculateMd5Hash($"{(isPost ? "POST" : "GET")}:{url}").ConfigureAwait(false);
        var digestResponse = await CalculateMd5Hash($"{ha1}:{nonce}:{++nc:00000000}:{cnonce}:{qop}:{ha2}").ConfigureAwait(false);
        return $"Digest username=\"{connection.UserName}\", realm=\"{realm}\", nonce=\"{nonce}\", uri=\"{url}\", " + $"algorithm=MD5, response=\"{digestResponse}\", qop={qop}, nc={nc:00000000}, cnonce=\"{cnonce}\"";
    }
}
