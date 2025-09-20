namespace De.Hochstaetter.Fronius.Services;

// ReSharper disable once CommentTypo
// Algorithm must be SHA256 (bug in 1.38.6-1), SHA-256 or MD5
// qop must be auth (auth-int and none are not supported)

public sealed class DigestAuthHttp : IDisposable, IAsyncDisposable
{
    private enum HashAlgorithm
    {
        None = 0,
        Sha256,
        Md5,
    }

    private readonly Lock hashLock = new();
    private readonly HttpClient httpClient;
    private readonly WebConnection connection;
    private readonly TimeSpan cnonceDuration;

    private HashAlgorithm hashAlgorithm = HashAlgorithm.None;
    private string? ha1;
    private string? realm;
    private string? nonce;
    private string? opaque;
    private string? cnonce;
    private string? algorithm;
    private Encoding? encoding;
    private DateTime cnonceDate;
    private uint nc;

    public DigestAuthHttp(WebConnection connection) : this(connection, new TimeSpan(0, 0, 1, 0)) { }

    public DigestAuthHttp(WebConnection connection, TimeSpan cnonceDuration)
    {
        this.connection = connection;
        this.cnonceDuration = cnonceDuration;

        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("HomeAutomationClient", "1.0"));
        httpClient.BaseAddress = new Uri(connection.BaseUrl);
    }

    ~DigestAuthHttp() => Dispose();

    public void Dispose()
    {
        httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

#pragma warning disable CA1816 // GC.SuppressFinalize(this) is guaranteed to be called in Dispose()
    public async ValueTask DisposeAsync()
    {
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        Dispose();
    }
#pragma warning restore CA1816

    public async ValueTask<(JToken, HttpStatusCode)> GetJsonToken(string url, JToken? jToken, IEnumerable<HttpStatusCode>? allowedStatusCodes = null, CancellationToken token = default)
    {
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        string? stringContent = null;

        if (jToken != null)
        {
            stringContent = jToken.ToString();
        }

        var (stringResult, httpStatusCode) = await GetString(url, stringContent, allowedStatusCodes, token).ConfigureAwait(false);
        JToken resultToken = new JObject();
        await Task.Run(() => { resultToken = JToken.Parse(stringResult); }, token).ConfigureAwait(false);

        return (resultToken, httpStatusCode);
    }

    public async ValueTask<(string, HttpStatusCode)> GetString(string url, string? stringContent = null, IEnumerable<HttpStatusCode>? allowedStatusCodes = null, CancellationToken token = default)
    {
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        using var response = await GetResponse(url, stringContent, allowedStatusCodes, token).ConfigureAwait(false);
        return (await response.Content.ReadAsStringAsync(token).ConfigureAwait(false), response.StatusCode);
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
            CreateRequest();
        }

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
            algorithm = GetAuthHeaderToken("algorithm");
            opaque = GetAuthHeaderToken("opaque");
            var qops = GetAuthHeaderToken("qop")?.Split(",") ?? [];

            if (!qops.Contains("auth"))
            {
                throw new NotSupportedException("Only qop=auth is supported");
            }

            hashAlgorithm = algorithm switch
            {
                "SHA-256" or "SHA256" => HashAlgorithm.Sha256,
                null or "MD5" => HashAlgorithm.Md5,
                _ => throw new NotSupportedException("Only SHA-256 and MD5 are supported"),
            };

            nc = 0;
            CreateRequest();
        }

        using (request)
        {
            response = await SendAsync(request, token).ConfigureAwait(false);
        }

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

        void CreateRequest()
        {
            request = new HttpRequestMessage(stringContent != null ? HttpMethod.Post : HttpMethod.Get, url);

            if (stringContent != null)
            {
                request.Content = new StringContent(stringContent, Encoding.UTF8, "application/json");
            }

            if (nc == uint.MaxValue || string.IsNullOrEmpty(nonce))
            {
                return;
            }

            if (DateTime.UtcNow - cnonceDate > cnonceDuration)
            {
                UpdateClientNonce();
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Digest", CreateDigestParameters());
            return;

            string CreateDigestParameters()
            {
                ha1 ??= CalculateHash($"{connection.UserName}:{realm}:{connection.Password}");
                var ha2 = CalculateHash($"{request.Method.Method}:{request.RequestUri?.OriginalString}");
                var digestResponse = CalculateHash($"{ha1}:{nonce}:{++nc:x8}:{cnonce}:auth:{ha2}");

                return $"username=\"{connection.UserName}\", " +
                       $"realm=\"{realm}\", nonce=\"{nonce}\", " +
                       $"uri=\"{request.RequestUri?.OriginalString}\", " +
                       $"response=\"{digestResponse}\", " +
                       (opaque != null ? $"opaque=\"{opaque}\", " : string.Empty) +
                       $"algorithm={algorithm}, qop=auth, nc={nc:x8}, cnonce=\"{cnonce}\"";

                [SuppressMessage("ReSharper", "SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault")]
                string CalculateHash(string input)
                {
                    var bytes = encoding!.GetBytes(input);

                    var hash = hashAlgorithm switch
                    {
                        HashAlgorithm.Sha256 => SHA256.HashData(bytes),
                        HashAlgorithm.Md5 => MD5.HashData(bytes),
                        _ => throw new NotSupportedException("Only SHA-256 and MD5 are supported"),
                    };

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

        void UpdateClientNonce()
        {
            cnonce = RandomNumberGenerator.GetHexString(16, true);
            cnonceDate = DateTime.UtcNow;
        }
    }


    private Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        return httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
    }
}
