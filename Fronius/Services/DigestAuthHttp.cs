using System.Text.RegularExpressions;

namespace De.Hochstaetter.Fronius.Services
{
    public class DigestAuthHttp
    {
        private readonly string host;
        private readonly string user;
        private readonly string password;
        private string? realm;
        private string? nonce;
        private string? qop;
        private string? cnonce;
        private DateTime cnonceDate;
        private int nc;

        public DigestAuthHttp(WebConnection connection)
        {
            host = connection.BaseUrl ?? "/";
            user = connection.UserName ?? "customer";
            password = connection.Password ?? string.Empty;
        }

        public async Task<string> GetString(string dir, JToken? token = null)
        {
            var isPost = token != null;
            dir = '/' + dir;
            var url = host + dir;
            var uri = new Uri(url);

#pragma warning disable CS0618
#pragma warning disable SYSLIB0014 // Type or member is obsolete
            var request = (HttpWebRequest)WebRequest.Create(uri);
#pragma warning restore SYSLIB0014 // Type or member is obsolete
#pragma warning restore CS0618
            await AddToken(request, token).ConfigureAwait(false);

            // If we've got a recent Auth header, re-use it!
            if (!string.IsNullOrEmpty(cnonce) && DateTime.Now.Subtract(cnonceDate).TotalHours < 1.0)
            {
                request.Headers.Add("Authorization", await CreateDigestHeader(dir, isPost).ConfigureAwait(false));
            }

            HttpWebResponse? response = null;

            try
            {
                try
                {
                    response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false);
                }
                catch (WebException ex)
                {
                    if (ex.Response == null || ((HttpWebResponse)ex.Response).StatusCode != HttpStatusCode.Unauthorized)
                    {
                        throw;
                    }

                    var wwwAuthenticateHeader = ex.Response.Headers["X-WWW-Authenticate"] ?? throw new UnauthorizedAccessException("No X-WWW-Authenticate header");
                    ex.Response.Dispose();
                    realm = await GetAuthHeaderToken("realm", wwwAuthenticateHeader).ConfigureAwait(false);
                    nonce = await GetAuthHeaderToken("nonce", wwwAuthenticateHeader).ConfigureAwait(false);
                    qop = await GetAuthHeaderToken("qop", wwwAuthenticateHeader).ConfigureAwait(false);

                    nc = 0;
                    cnonce = new Random().Next(123400, 9999999).ToString();
                    cnonceDate = DateTime.Now;
#pragma warning disable CS0618
#pragma warning disable SYSLIB0014 // Type or member is obsolete
                    var request2 = (HttpWebRequest)WebRequest.Create(uri);
                    await AddToken(request2, token).ConfigureAwait(false);
#pragma warning restore CS0618
#pragma warning restore SYSLIB0014 // Type or member is obsolete
                    request2.Headers.Add("Authorization", await CreateDigestHeader(dir, isPost).ConfigureAwait(false));
                    response = (HttpWebResponse)await request2.GetResponseAsync().ConfigureAwait(false);
                }

                using var reader = new StreamReader(response.GetResponseStream());
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
            finally
            {
                response?.Dispose();
            }
        }

        private async Task AddToken(HttpWebRequest request, JToken? token)
        {
            if (token == null)
            {
                return;
            }

            request.Method = "POST";
            request.ContentType = "application/json";
            await using var streamWriter = new StreamWriter(request.GetRequestStream());
            await streamWriter.WriteAsync(token.ToString()).ConfigureAwait(false);
        }

        private string CalculateMd5Hash(string input)
        {
            var inputBytes = Encoding.ASCII.GetBytes(input);
            var hash = MD5.Create().ComputeHash(inputBytes);
            var sb = new StringBuilder();

            foreach (var b in hash)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        private Task<string> GetAuthHeaderToken(string varName, string header) => Task.Run(() =>
        {
            var regHeader = new Regex($@"{varName}=""([^""]*)""");
            var matchHeader = regHeader.Match(header);

            if (matchHeader.Success)
            {
                return matchHeader.Groups[1].Value;
            }

            throw new ApplicationException($"Header {varName} not found");
        });

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private Task<string> CreateDigestHeader(string dir, bool isPost) => Task.Run(() =>
        {
            nc += 1;
            var ha1 = CalculateMd5Hash($"{user}:{realm}:{password}");
            var ha2 = CalculateMd5Hash($"{(isPost ? "POST" : "GET")}:{dir}");
            var digestResponse = CalculateMd5Hash($"{ha1}:{nonce}:{nc:00000000}:{cnonce}:{qop}:{ha2}");
            return $"Digest username=\"{user}\", realm=\"{realm}\", nonce=\"{nonce}\", uri=\"{dir}\", " + $"algorithm=MD5, response=\"{digestResponse}\", qop={qop}, nc={nc:00000000}, cnonce=\"{cnonce}\"";
        });
    }
}
