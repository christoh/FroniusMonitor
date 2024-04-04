namespace De.Hochstaetter.Fronius.Services;

// ReSharper disable once CommentTypo
// Algorithm must be MD5 (MD5-sess and SHA are not supported)
// qop must be auth (auth-int and none is not supported)

public sealed class DigestAuthHttp : IDisposable, IAsyncDisposable
{
    private static readonly Random random = new(unchecked((int)DateTime.UtcNow.Ticks));
    private readonly MD5 md5 = MD5.Create();
    private readonly HttpClient httpClient = new();
    private readonly WebConnection connection;
    private readonly TimeSpan cnonceDuration;
    private string? ha1;
    private string? realm;
    private string? nonce;
    private string? cnonce;
    private Encoding? encoding;
    private DateTime cnonceDate;
    private uint nc;

    public DigestAuthHttp(WebConnection connection, TimeSpan cnonceDuration)
    {
        this.connection = connection;
        this.cnonceDuration = cnonceDuration;
        httpClient.BaseAddress = new Uri(connection.BaseUrl);
    }

    public DigestAuthHttp(WebConnection connection) : this(connection, new TimeSpan(0, 1, 0, 0)) { }

    public void Dispose()
    {
        md5.Dispose();
        httpClient.Dispose();
    }

    public ValueTask DisposeAsync() => new(Task.Run(Dispose));

    public async ValueTask<(JToken, HttpStatusCode)> GetJsonToken(string url, JToken? jToken, IEnumerable<HttpStatusCode>? allowedStatusCodes = null, CancellationToken token = default)
    {
        string? stringContent = null;

        if (jToken != null)
        {
            await Task.Run(() => stringContent = jToken.ToString(), token).ConfigureAwait(false);
        }

        var (stringResult, httpStatusCode) = await GetString(url, stringContent, allowedStatusCodes, token).ConfigureAwait(false);
        JToken resultToken = new JObject();

        await Task.Run(() =>
        {
            resultToken = JToken.Parse(stringResult);
        }, token).ConfigureAwait(false);

        return (resultToken, httpStatusCode);
    }

    public async ValueTask<(string, HttpStatusCode)> GetString(string url, string? stringContent = null, IEnumerable<HttpStatusCode>? allowedStatusCodes = null, CancellationToken token = default)
    {
        var response = await GetResponse(url, stringContent, allowedStatusCodes, token).ConfigureAwait(false);

        try
        {
            return (await response.Content.ReadAsStringAsync(token).ConfigureAwait(false), response.StatusCode);
        }
        finally
        {
            response.Dispose();
        }
    }

    public async ValueTask<HttpResponseMessage> GetResponse(string url, string? stringContent, IEnumerable<HttpStatusCode>? allowedStatusCodesEnumerable = null, CancellationToken token = default)
    {
        allowedStatusCodesEnumerable ??= [HttpStatusCode.OK];
        var allowedStatusCodes = allowedStatusCodesEnumerable as IReadOnlyList<HttpStatusCode> ?? allowedStatusCodesEnumerable.ToList();

        if (url.Length == 0 || url[0] != '/')
        {
            url = '/' + url;
        }

        var request = await CreateRequest(url, stringContent, token).ConfigureAwait(false);

        try
        {
            var response = await httpClient.SendAsync(request, token).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                ThrowOnWrongStatusCode();
                return response;
            }

            request.Dispose();
            ha1 = null;

            var wwwAuthenticateHeader =
                response.Headers.GetValues("X-WWW-Authenticate").SingleOrDefault() ??
                response.Headers.GetValues("WWW-Authenticate").Single();

            response.Dispose();
            realm = await GetAuthHeaderToken("realm").ConfigureAwait(false) ?? string.Empty;
            nonce = await GetAuthHeaderToken("nonce").ConfigureAwait(false) ?? string.Empty;
            encoding = Encoding.GetEncoding(await GetAuthHeaderToken("charset").ConfigureAwait(false) ?? "UTF-8");
            var algorithm = await GetAuthHeaderToken("algorithm").ConfigureAwait(false);
            var qops = (await GetAuthHeaderToken("qop").ConfigureAwait(false))?.Split(",") ?? [];

            if (algorithm != "MD5" || !qops.Contains("auth"))
            {
                throw new NotSupportedException("Only MD5 with qop=auth is supported");
            }

            nc = 0;
            cnonce = unchecked((uint)random.Next(int.MinValue, int.MaxValue)).ToString("x8");
            cnonceDate = DateTime.UtcNow;
            request = await CreateRequest(url, stringContent, token).ConfigureAwait(false);
            response = await httpClient.SendAsync(request, token).ConfigureAwait(false);
            ThrowOnWrongStatusCode();
            return response;

            void ThrowOnWrongStatusCode()
            {
                if (!allowedStatusCodes.Contains(response.StatusCode))
                {
                    throw new HttpRequestException(response.ReasonPhrase, null, response.StatusCode);
                }
            }

            Task<string?> GetAuthHeaderToken(string varName) => Task.Run(() =>
            {
                var regex = new Regex($@"{varName}=(""([^""]*)""|([^,]*))");
                var matchHeader = regex.Match(wwwAuthenticateHeader);
                return !matchHeader.Success ? null : matchHeader.Groups.OfType<Group>().Last(group => group.Length > 0).Value;
            }, token);
        }
        finally
        {
            request.Dispose();
        }
    }

    private async ValueTask<HttpRequestMessage> CreateRequest(string url, string? stringContent, CancellationToken token)
    {
        var request = new HttpRequestMessage(stringContent != null ? HttpMethod.Post : HttpMethod.Get, url);


        if (stringContent != null)
        {
            await Task.Run(() => request.Content = new StringContent(stringContent, Encoding.UTF8, "application/json"), token).ConfigureAwait(false);
        }

        if (!string.IsNullOrEmpty(cnonce) && DateTime.UtcNow - cnonceDate < cnonceDuration && nc < 99_999_999)
        {
            request.Headers.Add("Authorization", await CreateDigestHeader(request, token).ConfigureAwait(false));
        }

        return request;
    }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private async ValueTask<string> CreateDigestHeader(HttpRequestMessage request, CancellationToken token)
    {
        encoding ??= Encoding.UTF8;
        ha1 ??= await CalculateMd5Hash($"{connection.UserName}:{realm}:{connection.Password}").ConfigureAwait(false);
        var ha2 = await CalculateMd5Hash($"{request.Method.Method}:{request.RequestUri?.OriginalString}").ConfigureAwait(false);
        var digestResponse = await CalculateMd5Hash($"{ha1}:{nonce}:{++nc:00000000}:{cnonce}:auth:{ha2}").ConfigureAwait(false);

        return $"Digest username=\"{connection.UserName}\", " +
               $"realm=\"{realm}\", nonce=\"{nonce}\", " +
               $"uri=\"{request.RequestUri?.OriginalString}\", " +
               $"algorithm=MD5, response=\"{digestResponse}\", " +
               $"qop=auth, nc={nc:00000000}, cnonce=\"{cnonce}\"";

        Task<string> CalculateMd5Hash(string input) => Task.Run(() =>
        {
            var hash = md5.ComputeHash(encoding.GetBytes(input));

            return hash
                .Aggregate
                (
                    new StringBuilder(hash.Length << 1),
                    (stringBuilder, hashByte) => stringBuilder.Append(hashByte.ToString("x2"))
                )
                .ToString();
        }, token);
    }
}
