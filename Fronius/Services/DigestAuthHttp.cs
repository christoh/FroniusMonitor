namespace De.Hochstaetter.Fronius.Services;

// Algorithm must be SHA256 (bug in 1.38.6-1), SHA-256 or MD5
// qop must be auth (auth-int and none are not supported)

public sealed class DigestAuthHttp(WebConnection connection, TimeSpan cnonceDuration) : IDisposable, IAsyncDisposable
{
    private enum HashAlgorithm
    {
        None = 0,
        Sha256,
        Md5,
    }

    private readonly Lock hashLock = new();

    private HashAlgorithm hashAlgorithm = HashAlgorithm.None;
    private string? hashA1;
    private string? realm;
    private string? nonce;
    private string? opaque;
    private string? cnonce;
    private string? algorithm;
    private Encoding? encoding;
    private DateTime cnonceDate;
    private uint nc;
    private bool isDisposed;
    private HttpClient? httpClient;

    public DigestAuthHttp(WebConnection connection) : this(connection, TimeSpan.FromMinutes(1)) { }

    private HttpClient HttpClient
    {
        get
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            lock (hashLock)
            {
                if (httpClient == null)
                {
                    httpClient = new()
                    {
                        BaseAddress = new(connection.BaseUrl),
                        Timeout = TimeSpan.FromSeconds(30),
                    };

                    httpClient.DefaultRequestHeaders.UserAgent.Add(new("HomeAutomationClient", FroniusGitInfo.Version.ToString()));
                }

                return httpClient;
            }
        }
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        httpClient?.Dispose();
        httpClient = null;
        isDisposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        Dispose();
    }

    public async ValueTask<(JsonNode, HttpStatusCode)> GetJsonToken(string url, JsonNode? jToken, IEnumerable<HttpStatusCode>? allowedStatusCodes = null, CancellationToken token = default)
    {
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        var stringContent = jToken?.ToJsonString();

        var (stringResult, httpStatusCode) = await GetString(url, stringContent, allowedStatusCodes, token).ConfigureAwait(false);
        JsonNode resultToken = new JsonObject();

        // Off the caller's thread, as before: parsing the config of an inverter is not a small piece of work.
        // JsonNode.Parse answers null for a body of literally "null", which is nothing to hand on.
        await Task.Run(() => { resultToken = JsonNode.Parse(stringResult) ?? new JsonObject(); }, token).ConfigureAwait(false);

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
        var allowedStatusCodes = allowedStatusCodesEnumerable as IReadOnlyList<HttpStatusCode> ?? [.. allowedStatusCodesEnumerable];

        if (url.Length == 0 || url[0] != '/')
        {
            url = '/' + url;
        }

        HttpRequestMessage request;

        lock (hashLock)
        {
            CreateRequest();
        }

        HttpResponseMessage response;

        try
        {
            response = await SendAsync(request, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            request.Dispose();

            lock (hashLock)
            {
                httpClient?.Dispose();
                httpClient = null;
                CreateRequest();
            }

            response = await SendAsync(request, token).ConfigureAwait(false);
        }

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            request.Dispose();
            ThrowOnWrongStatusCode();
            return response;
        }

        request.Dispose();

        if (!response.Headers.TryGetValues("X-WWW-Authenticate", out var authValues))
        {
            response.Headers.TryGetValues("WWW-Authenticate", out authValues);
        }

        var wwwAuthenticateHeader = authValues?.SingleOrDefault()
            ?? throw new HttpRequestException("No WWW-Authenticate header in 401 response", null, response.StatusCode);

        lock (hashLock)
        {
            hashA1 = null;
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
                hashA1 ??= CalculateHash($"{connection.UserName}:{realm}:{connection.Password}");
                var hashA2 = CalculateHash($"{request.Method.Method}:{request.RequestUri?.OriginalString}");
                var digestResponse = CalculateHash($"{hashA1}:{nonce}:{++nc:x8}:{cnonce}:auth:{hashA2}");

                return $"""username="{connection.UserName}", """ +
                       $"""realm="{realm}", nonce="{nonce}", """ +
                       $"""uri="{request.RequestUri?.OriginalString}", """ +
                       $"""response="{digestResponse}", """ +
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
        return HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
    }
}
