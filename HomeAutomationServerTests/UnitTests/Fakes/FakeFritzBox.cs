using System.Diagnostics;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Linq;
using De.Hochstaetter.FritzBox.Http;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

/// <summary>
/// One request as <see cref="FakeFritzBox"/> received it, with the path and query as the box sees them. The body is
/// read right away, because the request is disposed afterwards.
/// </summary>
/// <param name="Method">The HTTP method.</param>
/// <param name="PathAndQuery">The path with the query, for example <c>/webservices/homeautoswitch.lua?switchcmd=getdevicelistinfos</c>.</param>
/// <param name="Authorization">The <c>Authorization</c> header, or <see langword="null"/> if there was none.</param>
/// <param name="ContentType">The media type of the body, or <see langword="null"/> if the request had no content.</param>
/// <param name="Body">The body as text, empty if the request had no content.</param>
/// <param name="Timestamp">When the box received the request, as <see cref="Stopwatch.GetTimestamp"/> counts it.</param>
internal sealed record FakeFritzBoxCall(HttpMethod Method, string PathAndQuery, string? Authorization, string? ContentType, string Body, long Timestamp)
{
    /// <summary>The method and the path with the query, as in <c>GET /jason_boxinfo.xml</c>.</summary>
    public string Line => $"{Method} {PathAndQuery}";

    /// <summary>The path without the query.</summary>
    public string Path => PathAndQuery.Split('?')[0];

    /// <summary>The query without the leading <c>?</c>, empty if there is none.</summary>
    public string Query => PathAndQuery.Contains('?', StringComparison.Ordinal) ? PathAndQuery[(PathAndQuery.IndexOf('?', StringComparison.Ordinal) + 1)..] : string.Empty;

    /// <summary>A field of a form-encoded body.</summary>
    public string? Field(string name) => HttpUtility.ParseQueryString(Body)[name];

    public bool IsLogin => PathAndQuery == FakeFritzBox.LoginPath;

    public bool IsBoxInfo => Path == FakeFritzBox.BoxInfoPath;

    public bool IsRest => Path.StartsWith(FakeFritzBox.RestPrefix, StringComparison.Ordinal);

    public bool IsAha => Path == FakeFritzBox.AhaPath;

    /// <summary>A request of the Smart Home REST API or of AHA, as against the login and the box info.</summary>
    public bool IsApi => IsRest || IsAha;
}

/// <summary>
/// The part of a FRITZ!Box that every test of the web interface needs, so that no test brings its own: it records
/// every request in the order it arrived and implements <c>login_sid.lua?version=2</c> the way FRITZ!OS does. A GET
/// answers with the challenge and no session, a POST with the user and the response answers with a new session
/// (<see cref="Sid"/>) if the login is accepted and with no session if it is not. Every other request goes to
/// <see cref="Answer"/>, which is the part that differs between the tests and is not asked about the session. The
/// defaults suit a test that does not care about the login: any response is accepted.
/// </summary>
internal abstract class FakeFritzBox : HttpMessageHandler
{
    /// <summary>The login request: the only path the base class answers by itself.</summary>
    public const string LoginPath = "/login_sid.lua?version=2";

    public const string BoxInfoPath = "/jason_boxinfo.xml";

    public const string AhaPath = "/webservices/homeautoswitch.lua";

    /// <summary>What every path of the Smart Home REST API starts with.</summary>
    public const string RestPrefix = "/api/v0/";

    /// <summary>The session that tells the client that it is not logged in.</summary>
    public const string NoSession = "0000000000000000";

    /// <summary>The address the tests give the box.</summary>
    public const string BaseUrl = "http://192.168.44.11";

    public static readonly Uri BaseUri = new(BaseUrl);

    private readonly Lock gate = new();
    private readonly List<FakeFritzBoxCall> calls = [];
    private int logins;
    private int disposals;

    /// <summary>The challenge the box sends: PBKDF2 (<c>2$...</c>) or MD5.</summary>
    public string Challenge { get; init; } = "2$10$5A1711$10$5A1722";

    /// <summary>
    /// The password a login is checked against: the form content type, a user of <see cref="UsersXml"/> and the response
    /// <see cref="FritzBoxLogin.CreateResponse"/> makes of <see cref="Challenge"/> and this password. <see langword="null"/>
    /// accepts any response, from any user.
    /// </summary>
    public string? Password { get; init; }

    /// <summary>The block time in seconds the box reports, which the client has to wait out before it sends a response.</summary>
    public int BlockTime { get; init; }

    /// <summary>The children of <c>&lt;Users&gt;</c>.</summary>
    public string UsersXml { get; init; } = """<User last="1">user</User>""";

    /// <summary>How long the box takes to answer a login request.</summary>
    public TimeSpan LoginDelay { get; init; }

    /// <summary>The box answers a login with no session, as it does for a wrong password, whatever the response is.</summary>
    public bool RefuseLogin { get; init; }

    /// <summary>Runs when a request has been recorded and before it is answered. A test can hold a request with it.</summary>
    public Func<FakeFritzBoxCall, CancellationToken, Task>? Before { get; init; }

    /// <summary>How often the handler was disposed, which a <see cref="HttpClient"/> does with the handler it owns.</summary>
    public int Disposals => Volatile.Read(ref disposals);

    /// <summary>Every request in the order it arrived, the login included.</summary>
    public IReadOnlyList<FakeFritzBoxCall> Calls
    {
        get
        {
            lock (gate)
            {
                return [.. calls];
            }
        }
    }

    /// <summary>Everything but the login.</summary>
    public IReadOnlyList<FakeFritzBoxCall> Requests => [.. Calls.Where(c => !c.IsLogin)];

    public IReadOnlyList<FakeFritzBoxCall> LoginGets => [.. Calls.Where(c => c.IsLogin && c.Method == HttpMethod.Get)];

    public IReadOnlyList<FakeFritzBoxCall> LoginPosts => [.. Calls.Where(c => c.IsLogin && c.Method == HttpMethod.Post)];

    /// <summary>The session the n-th successful login (counting from 1) gets.</summary>
    public static string Sid(int login) => (0x0123456789abcdefUL + (ulong)(login - 1)).ToString("x16");

    /// <summary>The answer to a request that is not a login.</summary>
    /// <param name="call">The request.</param>
    /// <param name="number">How many requests that are not logins have arrived, this one included (counting from 1).</param>
    protected abstract HttpResponseMessage Answer(FakeFritzBoxCall call, int number);

    protected static HttpResponseMessage Reply(HttpStatusCode status, string body, string contentType) => new(status) { Content = new StringContent(body, Encoding.UTF8, contentType) };

    protected sealed override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var call = new FakeFritzBoxCall
        (
            request.Method,
            request.RequestUri!.PathAndQuery,
            request.Headers.TryGetValues("Authorization", out var authorization) ? string.Join(", ", authorization) : null,
            request.Content?.Headers.ContentType?.MediaType,
            request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false),
            Stopwatch.GetTimestamp()
        );

        int number;

        lock (gate)
        {
            calls.Add(call);
            number = calls.Count(c => !c.IsLogin);
        }

        if (Before != null)
        {
            await Before(call, cancellationToken).ConfigureAwait(false);
        }

        if (!call.IsLogin)
        {
            return Answer(call, number);
        }

        if (LoginDelay > TimeSpan.Zero)
        {
            await Task.Delay(LoginDelay, cancellationToken).ConfigureAwait(false);
        }

        if (call.Method == HttpMethod.Get)
        {
            return SessionInfo(NoSession);
        }

        if (call.Method == HttpMethod.Post)
        {
            return SessionInfo(IsAccepted(call) ? Sid(Interlocked.Increment(ref logins)) : NoSession);
        }

        throw new InvalidOperationException($"The fake box knows nothing about {call.Line}");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Interlocked.Increment(ref disposals);
        }

        base.Dispose(disposing);
    }

    private bool IsAccepted(FakeFritzBoxCall call)
    {
        if (RefuseLogin)
        {
            return false;
        }

        if (Password == null)
        {
            return true;
        }

        var users = XElement.Parse($"<Users>{UsersXml}</Users>").Elements("User").Select(u => u.Value);

        return call.ContentType == "application/x-www-form-urlencoded" &&
               call.Field("username") is { } user && users.Contains(user) &&
               call.Field("response") == FritzBoxLogin.CreateResponse(Challenge, Password);
    }

    private HttpResponseMessage SessionInfo(string sid) => Reply
    (
        HttpStatusCode.OK,
        $"""<?xml version="1.0" encoding="utf-8"?><SessionInfo><SID>{sid}</SID><Challenge>{Challenge}</Challenge><BlockTime>{BlockTime}</BlockTime><Rights></Rights><Users>{UsersXml}</Users></SessionInfo>""",
        "text/xml"
    );
}
