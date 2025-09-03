namespace De.Hochstaetter.Fronius.Services;

// ReSharper disable once CommentTypo
// Algorithm must be SHA256
// qop must be auth (auth-int and none is not supported)

public sealed class DigestAuthHttp(WebConnection connection, TimeSpan cnonceDuration) : IDisposable, IAsyncDisposable
{
    private static readonly Random random = new(unchecked((int)DateTime.UtcNow.Ticks));
    private readonly Lock hashLock = new();

    private readonly SHA256 sha256 = SHA256.Create();
    //private readonly HttpClient httpClient = new();

    private string? ha1;
    private string? realm;
    private string? nonce;
    private string? cnonce;
    private Encoding? encoding;
    private DateTime cnonceDate;
    private uint nc;

    public DigestAuthHttp(WebConnection connection) : this(connection, new TimeSpan(0, 1, 0, 0)) { }

    public void Dispose()
    {
        sha256.Dispose();
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

        await Task.Run(() => { resultToken = JToken.Parse(stringResult); }, token).ConfigureAwait(false);

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

        HttpRequestMessage request;

        lock (hashLock)
        {
            request = CreateRequest(url, stringContent, token);
        }

        try
        {
            var response = await SendAsync(request, token).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                ThrowOnWrongStatusCode();
                return response;
            }

            request.Dispose();

            var wwwAuthenticateHeader =
                response.Headers.GetValues("X-WWW-Authenticate").SingleOrDefault() ??
                response.Headers.GetValues("WWW-Authenticate").Single();

            lock (hashLock)
            {
                ha1 = null;
                response.Dispose();
                realm = GetAuthHeaderToken("realm") ?? string.Empty;
                nonce = GetAuthHeaderToken("nonce") ?? string.Empty;
                encoding = Encoding.GetEncoding(GetAuthHeaderToken("charset") ?? "UTF-8");
                var algorithm = GetAuthHeaderToken("algorithm");
                var qops = (GetAuthHeaderToken("qop"))?.Split(",") ?? [];

                if (algorithm != "SHA256" || !qops.Contains("auth"))
                {
                    throw new NotSupportedException("Only SHA256 with qop=auth is supported");
                }

                nc = 0;
                cnonce = unchecked((uint)random.Next(int.MinValue, int.MaxValue)).ToString("x8") + unchecked((uint)random.Next(int.MinValue, int.MaxValue)).ToString("x8");
                cnonceDate = DateTime.UtcNow;
                request = CreateRequest(url, stringContent, token);
            }
            response = await SendAsync(request, token).ConfigureAwait(false);
            ThrowOnWrongStatusCode();
            return response;

            void ThrowOnWrongStatusCode()
            {
                if (!allowedStatusCodes.Contains(response.StatusCode))
                {
                    throw new HttpRequestException(response.ReasonPhrase, null, response.StatusCode);
                }
            }

            string? GetAuthHeaderToken(string varName)
            {
                var regex = new Regex($"{varName}=(\"([^\"]*)\"|([^,]*))");
                var matchHeader = regex.Match(wwwAuthenticateHeader);
                return !matchHeader.Success ? null : matchHeader.Groups.OfType<Group>().Last(group => group.Length > 0).Value;
            }
        }
        finally
        {
            request.Dispose();
        }
    }

    private Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(connection.BaseUrl);
        _ = Task.Delay(TimeSpan.FromSeconds(30), CancellationToken.None).ContinueWith(_ => { httpClient.Dispose(); }, CancellationToken.None);
        return httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
    }

    private HttpRequestMessage CreateRequest(string url, string? stringContent, CancellationToken token)
    {
        var request = new HttpRequestMessage(stringContent != null ? HttpMethod.Post : HttpMethod.Get, url);


        if (stringContent != null)
        {
            request.Content = new StringContent(stringContent, Encoding.UTF8, "application/json");
        }

        if (!string.IsNullOrEmpty(cnonce) && DateTime.UtcNow - cnonceDate < cnonceDuration && nc < 99_999_999)
        {
            request.Headers.Add("Authorization", CreateDigestHeader(request, token));
        }

        return request;
    }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private string CreateDigestHeader(HttpRequestMessage request, CancellationToken token)
    {
        encoding ??= Encoding.UTF8;
        ha1 ??= CalculateHash($"{connection.UserName}:{realm}:{connection.Password}");
        var ha2 = CalculateHash($"{request.Method.Method}:{request.RequestUri?.OriginalString}");
        var digestResponse = CalculateHash($"{ha1}:{nonce}:{++nc:00000000}:{cnonce}:auth:{ha2}");

        return $"Digest username=\"{connection.UserName}\", " +
               $"realm=\"{realm}\", nonce=\"{nonce}\", " +
               $"uri=\"{request.RequestUri?.OriginalString}\", " +
               $"response=\"{digestResponse}\", " +
               $"qop=auth, nc={nc:00000000}, cnonce=\"{cnonce}\"";

        string CalculateHash(string input)
        {
            var hash = sha256.ComputeHash(encoding.GetBytes(input));

            return hash
                .Aggregate
                (
                    new StringBuilder(hash.Length << 1),
                    (stringBuilder, hashByte) => stringBuilder.Append(hashByte.ToString("x2"))
                )
                .ToString();
        }
    }
}
