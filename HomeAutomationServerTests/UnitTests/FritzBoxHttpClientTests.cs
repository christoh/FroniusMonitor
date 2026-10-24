using System.Diagnostics;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Linq;
using De.Hochstaetter.FritzBox.Http;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The web interface side of the FritzBox library: <see cref="FritzBoxLogin"/> against the test vectors AVM publishes
/// in its "Session ID" technical note, and <see cref="FritzBoxHttpClient"/> against a fake box that implements
/// <c>login_sid.lua?version=2</c> the way FRITZ!OS does - the session handling, the AHA and REST requests, the new
/// login after an expired session, the block time after wrong passwords and the error mapping.
/// </summary>
public sealed class FritzBoxHttpClientTests : IDisposable
{
    private const string DefaultPassword = "1example!";
    private const string LoginPath = "/login_sid.lua?version=2";
    private const string OverviewPath = "/api/v0/smarthome/overview";
    private const string Overview = """{"units":[{"UID":"11630 0015376","name":"Desk lamp"}]}""";
    private const string DeviceList = """<devicelist version="1"><device identifier="11630 0015376"/></devicelist>""";
    private const string LastUserFirst = """<User last="1">christoh</User><User>other</User>""";
    private const string LastUserSecond = """<User>other</User><User last="1">christoh</User>""";

    private static readonly TimeSpan defaultMaximumBlockTime = new FritzBoxHttpClientOptions { BaseUri = FakeBox.BaseUri, Password = string.Empty }.MaximumBlockTime;

    private readonly ILoggerFactory loggers = LoggerFactory.Create(builder => builder.AddTestOutput());

    public void Dispose() => loggers.Dispose();

    #region FritzBoxLogin

    [Fact]
    public void The_PBKDF2_response_is_the_one_AVM_publishes_for_its_test_vector()
    {
        var response = FritzBoxLogin.CreateResponse("2$10000$5A1711$2000$5A1722", "1example!");

        Assert.Equal("5A1722$1798a1672bca7c6463d6b245f82b53703b0f50813401b03e4045a5861e689adb", response);
    }

    [Fact]
    public void The_MD5_response_is_the_one_AVM_publishes_for_its_test_vector()
    {
        var response = FritzBoxLogin.CreateResponse("1234567z", "äbc");

        Assert.Equal("1234567z-9e224a41eeefa284df7bb0f26c2913e2", response);
    }

    [Theory]
    [InlineData("pass€word", "pass.word")]
    [InlineData("日本語", "...")]
    [InlineData("Āb", ".b")]
    public void The_MD5_response_treats_a_character_above_U00FF_as_a_dot(string password, string passwordWithDots)
    {
        Assert.Equal(FritzBoxLogin.CreateResponse("1234567z", passwordWithDots), FritzBoxLogin.CreateResponse("1234567z", password));
    }

    [Fact]
    public void The_MD5_response_keeps_U00FF_which_is_the_last_character_that_is_not_replaced()
    {
        var withY = FritzBoxLogin.CreateResponse("1234567z", "aÿb");
        var withDot = FritzBoxLogin.CreateResponse("1234567z", "a.b");

        Assert.NotEqual(withDot, withY);
    }

    [Theory]
    [InlineData("2$10000$5A1711$2000")]
    [InlineData("2$10000$5A1711$2000$5A1722$surplus")]
    [InlineData("2$")]
    public void A_PBKDF2_challenge_with_the_wrong_number_of_parts_throws_an_InvalidDataException(string challenge)
    {
        var exception = Assert.Throws<InvalidDataException>(() => FritzBoxLogin.CreateResponse(challenge, "1example!"));

        Assert.Contains($"{challenge.Count(c => c == '$') + 1} parts", exception.Message);
    }

    #endregion

    #region FritzBoxHttpClient: requests

    [Fact]
    public async Task A_REST_GET_logs_in_once_and_sends_the_session_in_the_Authorization_header()
    {
        var box = new FakeBox();
        using var client = CreateClient(box);

        var answer = await client.GetRestAsync("smarthome/overview", TestContext.Current.CancellationToken);

        Assert.Equal(Overview, answer);
        Assert.Equal([$"GET {LoginPath}", $"POST {LoginPath}", $"GET {OverviewPath}"], box.Calls.Select(c => $"{c.Method} {c.PathAndQuery}"));
        Assert.Equal($"AVM-SID {FakeBox.Sid(1)}", Assert.Single(box.ApiCalls).Authorization);
    }

    [Fact]
    public async Task The_login_answers_the_challenge_with_the_PBKDF2_response_AVM_publishes_as_a_form()
    {
        var box = new FakeBox();
        using var client = CreateClient(box);

        await client.GetRestAsync("smarthome/overview", TestContext.Current.CancellationToken);

        var post = Assert.Single(box.LoginPosts);
        Assert.Equal("application/x-www-form-urlencoded", post.ContentType);
        Assert.Equal("christoh", post.Field("username"));
        Assert.Equal("5A1722$1798a1672bca7c6463d6b245f82b53703b0f50813401b03e4045a5861e689adb", post.Field("response"));
    }

    [Fact]
    public async Task A_box_with_an_MD5_challenge_is_answered_with_the_MD5_response()
    {
        var box = new FakeBox { Challenge = "1234567z", Password = "äbc" };
        using var client = CreateClient(box, Options(password: "äbc"));

        var answer = await client.GetRestAsync("smarthome/overview", TestContext.Current.CancellationToken);

        Assert.Equal(Overview, answer);
        Assert.Equal("1234567z-9e224a41eeefa284df7bb0f26c2913e2", Assert.Single(box.LoginPosts).Field("response"));
    }

    [Fact]
    public async Task A_leading_slash_of_the_REST_path_is_ignored()
    {
        var box = new FakeBox();
        using var client = CreateClient(box);

        await client.GetRestAsync("/smarthome/overview", TestContext.Current.CancellationToken);

        Assert.Equal(OverviewPath, Assert.Single(box.ApiCalls).PathAndQuery);
    }

    [Fact]
    public async Task An_AHA_request_carries_the_query_and_the_session_in_the_URL()
    {
        var box = new FakeBox { Respond = _ => (HttpStatusCode.OK, DeviceList) };
        using var client = CreateClient(box);

        var answer = await client.GetAhaAsync("switchcmd=getdevicelistinfos", TestContext.Current.CancellationToken);

        Assert.Equal(DeviceList, answer);
        var call = Assert.Single(box.ApiCalls);
        Assert.Equal(HttpMethod.Get, call.Method);
        Assert.Equal($"/webservices/homeautoswitch.lua?switchcmd=getdevicelistinfos&sid={FakeBox.Sid(1)}", call.PathAndQuery);
        Assert.Null(call.Authorization);
    }

    [Fact]
    public async Task A_REST_PUT_sends_the_JSON_with_its_content_type_to_the_escaped_path()
    {
        const string json = """{"name":"Desk lamp"}""";
        var box = new FakeBox();
        using var client = CreateClient(box);

        await client.SendRestAsync(HttpMethod.Put, "smarthome/overview/units/11630 0015376", json, TestContext.Current.CancellationToken);

        var call = Assert.Single(box.ApiCalls);
        Assert.Equal(HttpMethod.Put, call.Method);
        Assert.Equal("/api/v0/smarthome/overview/units/11630%200015376", call.PathAndQuery);
        Assert.Equal("application/json", call.ContentType);
        Assert.Equal(json, call.Body);
        Assert.Equal($"AVM-SID {FakeBox.Sid(1)}", call.Authorization);
    }

    [Fact]
    public async Task The_session_is_reused_by_the_later_calls_of_every_kind()
    {
        var box = new FakeBox();
        using var client = CreateClient(box);
        var token = TestContext.Current.CancellationToken;

        await client.GetRestAsync("smarthome/overview", token);
        await client.GetAhaAsync("switchcmd=getdevicelistinfos", token);
        await client.SendRestAsync(HttpMethod.Put, "smarthome/overview/units/11630 0015376", "{}", token);

        Assert.Single(box.LoginGets);
        Assert.Single(box.LoginPosts);
        Assert.Equal(3, box.ApiCalls.Count);
        Assert.Equal($"AVM-SID {FakeBox.Sid(1)}", box.ApiCalls[0].Authorization);
        Assert.Contains($"sid={FakeBox.Sid(1)}", box.ApiCalls[1].PathAndQuery);
        Assert.Equal($"AVM-SID {FakeBox.Sid(1)}", box.ApiCalls[2].Authorization);
    }

    [Fact]
    public async Task Several_concurrent_first_calls_log_in_only_once()
    {
        // The delay keeps the first login running while the other calls arrive, so that they really have to wait for it.
        var box = new FakeBox { LoginDelay = TimeSpan.FromMilliseconds(200) };
        using var client = CreateClient(box);
        var token = TestContext.Current.CancellationToken;

        var answers = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => client.GetRestAsync("smarthome/overview", token), token)));

        Assert.All(answers, answer => Assert.Equal(Overview, answer));
        Assert.Single(box.LoginGets);
        Assert.Single(box.LoginPosts);
        Assert.Equal(8, box.ApiCalls.Count);
        Assert.All(box.ApiCalls, call => Assert.Equal($"AVM-SID {FakeBox.Sid(1)}", call.Authorization));
    }

    #endregion

    #region FritzBoxHttpClient: the user

    [Theory]
    [InlineData(null, LastUserFirst)]
    [InlineData(null, LastUserSecond)]
    [InlineData("", LastUserSecond)]
    public async Task Without_a_configured_user_the_user_the_box_marks_as_last_one_logs_in(string? userName, string users)
    {
        var box = new FakeBox { UsersXml = users };
        using var client = CreateClient(box, Options(userName));

        await client.GetRestAsync("smarthome/overview", TestContext.Current.CancellationToken);

        Assert.Equal("christoh", Assert.Single(box.LoginPosts).Field("username"));
    }

    [Theory]
    [InlineData(LastUserFirst)]
    [InlineData(LastUserSecond)]
    public async Task A_configured_user_logs_in_instead_of_the_one_the_box_marks_as_last_one(string users)
    {
        var box = new FakeBox { UsersXml = users };
        using var client = CreateClient(box, Options("other"));

        await client.GetRestAsync("smarthome/overview", TestContext.Current.CancellationToken);

        Assert.Equal("other", Assert.Single(box.LoginPosts).Field("username"));
    }

    #endregion

    #region FritzBoxHttpClient: expired sessions and errors

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task A_session_the_box_no_longer_accepts_is_replaced_and_the_request_is_repeated_once(HttpStatusCode refusal)
    {
        var box = new FakeBox { Respond = number => number == 1 ? (refusal, "session expired") : (HttpStatusCode.OK, Overview) };
        using var client = CreateClient(box);

        var answer = await client.GetRestAsync("smarthome/overview", TestContext.Current.CancellationToken);

        Assert.Equal(Overview, answer);
        Assert.Equal(2, box.LoginGets.Count);
        Assert.Equal(2, box.LoginPosts.Count);
        Assert.Equal([$"AVM-SID {FakeBox.Sid(1)}", $"AVM-SID {FakeBox.Sid(2)}"], box.ApiCalls.Select(c => c.Authorization));
    }

    [Fact]
    public async Task An_AHA_request_that_is_repeated_after_a_new_login_carries_the_new_session()
    {
        var box = new FakeBox { Respond = number => number == 1 ? (HttpStatusCode.Forbidden, string.Empty) : (HttpStatusCode.OK, DeviceList) };
        using var client = CreateClient(box);

        var answer = await client.GetAhaAsync("switchcmd=getdevicelistinfos", TestContext.Current.CancellationToken);

        Assert.Equal(DeviceList, answer);
        Assert.Equal
        (
            [
                $"/webservices/homeautoswitch.lua?switchcmd=getdevicelistinfos&sid={FakeBox.Sid(1)}",
                $"/webservices/homeautoswitch.lua?switchcmd=getdevicelistinfos&sid={FakeBox.Sid(2)}",
            ],
            box.ApiCalls.Select(c => c.PathAndQuery)
        );
    }

    [Fact]
    public async Task A_REST_PUT_that_is_repeated_after_a_new_login_sends_its_body_again()
    {
        const string json = """{"name":"Desk lamp"}""";
        var box = new FakeBox { Respond = number => number == 1 ? (HttpStatusCode.Unauthorized, string.Empty) : (HttpStatusCode.OK, string.Empty) };
        using var client = CreateClient(box);

        await client.SendRestAsync(HttpMethod.Put, "smarthome/overview/units/11630 0015376", json, TestContext.Current.CancellationToken);

        Assert.Equal(2, box.ApiCalls.Count);
        Assert.All(box.ApiCalls, call =>
        {
            Assert.Equal(HttpMethod.Put, call.Method);
            Assert.Equal(json, call.Body);
            Assert.Equal("application/json", call.ContentType);
        });

        Assert.Equal($"AVM-SID {FakeBox.Sid(2)}", box.ApiCalls[1].Authorization);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task A_box_that_keeps_refusing_the_new_session_ends_in_an_HttpRequestException_after_one_repeat(HttpStatusCode refusal)
    {
        var box = new FakeBox { Respond = _ => (refusal, "no access") };
        using var client = CreateClient(box);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetRestAsync("smarthome/overview", TestContext.Current.CancellationToken));

        Assert.Equal(refusal, exception.StatusCode);
        Assert.Contains("no access", exception.Message);
        Assert.Equal(2, box.ApiCalls.Count);
        Assert.Equal(2, box.LoginPosts.Count);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, "lua: stack overflow")]
    [InlineData(HttpStatusCode.NotFound, "no such unit")]
    [InlineData(HttpStatusCode.BadRequest, "invalid JSON")]
    public async Task Any_other_error_status_throws_an_HttpRequestException_with_the_answer_of_the_box_and_is_not_repeated(HttpStatusCode status, string body)
    {
        var box = new FakeBox { Respond = _ => (status, body) };
        using var client = CreateClient(box);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetRestAsync("smarthome/overview", TestContext.Current.CancellationToken));

        Assert.Equal(status, exception.StatusCode);
        Assert.Contains(body, exception.Message);
        Assert.Contains(OverviewPath, exception.Message);
        Assert.Single(box.ApiCalls);
        Assert.Single(box.LoginPosts);
    }

    [Fact]
    public async Task An_error_status_of_a_REST_PUT_is_thrown_too()
    {
        var box = new FakeBox { Respond = _ => (HttpStatusCode.BadRequest, "invalid JSON") };
        using var client = CreateClient(box);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendRestAsync(HttpMethod.Put, "smarthome/overview/units/11630 0015376", "{", TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Contains("invalid JSON", exception.Message);
    }

    #endregion

    #region FritzBoxHttpClient: failed logins

    [Fact]
    public async Task A_wrong_password_throws_an_UnauthorizedAccessException_before_any_request_is_sent()
    {
        var box = new FakeBox();
        using var client = CreateClient(box, Options(password: "wrong"));

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => client.GetRestAsync("smarthome/overview", TestContext.Current.CancellationToken));

        Assert.Contains("christoh", exception.Message);
        Assert.Single(box.LoginPosts);
        Assert.Empty(box.ApiCalls);
    }

    [Fact]
    public async Task A_failed_login_is_not_remembered_and_the_next_call_logs_in_again()
    {
        var box = new FakeBox();
        using var client = CreateClient(box, Options(password: "wrong"));
        var token = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => client.GetRestAsync("smarthome/overview", token));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => client.GetRestAsync("smarthome/overview", token));

        Assert.Equal(2, box.LoginPosts.Count);
        Assert.Empty(box.ApiCalls);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(31, 30)]
    public async Task A_block_time_above_the_maximum_throws_an_UnauthorizedAccessException_without_sending_a_response(int blockSeconds, int maximumSeconds)
    {
        var box = new FakeBox { BlockTime = blockSeconds };
        using var client = CreateClient(box, Options(maximumBlockTime: TimeSpan.FromSeconds(maximumSeconds)));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => client.GetRestAsync("smarthome/overview", TestContext.Current.CancellationToken));

        Assert.Single(box.LoginGets);
        Assert.Empty(box.LoginPosts);
        Assert.Empty(box.ApiCalls);
    }

    [Fact]
    public async Task A_block_time_that_does_not_exceed_the_maximum_is_waited_out_before_the_response_is_sent()
    {
        // The block time equals the maximum, so this covers the boundary as well.
        var box = new FakeBox { BlockTime = 1 };
        using var client = CreateClient(box, Options(maximumBlockTime: TimeSpan.FromSeconds(1)));

        var answer = await client.GetRestAsync("smarthome/overview", TestContext.Current.CancellationToken);

        Assert.Equal(Overview, answer);
        var waited = Stopwatch.GetElapsedTime(Assert.Single(box.LoginGets).Timestamp, Assert.Single(box.LoginPosts).Timestamp);
        Assert.True(waited >= TimeSpan.FromMilliseconds(900), $"The response was sent {waited.TotalMilliseconds:N0} ms after the challenge was fetched, which is less than the block time of 1 s");
    }

    #endregion

    private static FritzBoxHttpClientOptions Options(string? userName = null, string password = DefaultPassword, TimeSpan? maximumBlockTime = null) => new()
    {
        BaseUri = FakeBox.BaseUri,
        UserName = userName,
        Password = password,
        MaximumBlockTime = maximumBlockTime ?? defaultMaximumBlockTime,
    };

    private FritzBoxHttpClient CreateClient(FakeBox box, FritzBoxHttpClientOptions? options = null) => new(options ?? Options(), loggers.CreateLogger<FritzBoxHttpClient>(), box);

    /// <summary>One request as the fake box received it. The body is read right away, because the request is disposed afterwards.</summary>
    private sealed record Call(HttpMethod Method, string PathAndQuery, string? Authorization, string? ContentType, string Body, long Timestamp)
    {
        public bool IsLogin => PathAndQuery == LoginPath;

        /// <summary>A field of a form-encoded body.</summary>
        public string? Field(string name) => HttpUtility.ParseQueryString(Body)[name];
    }

    /// <summary>
    /// <c>login_sid.lua?version=2</c> the way FRITZ!OS implements it: a GET answers with the challenge and no session,
    /// a POST with the user and the response answers with a session if the response is right. Every other request goes
    /// to <see cref="Respond"/>, which is told the number of the request and is not asked about the session. All
    /// requests are recorded, in the order they arrived.
    /// </summary>
    private sealed class FakeBox : HttpMessageHandler
    {
        public const string NoSession = "0000000000000000";

        public static readonly Uri BaseUri = new("http://192.168.44.11");

        private readonly Lock gate = new();
        private readonly List<Call> calls = [];
        private int logins;

        public string Challenge { get; init; } = "2$10000$5A1711$2000$5A1722";

        public string Password { get; init; } = DefaultPassword;

        public int BlockTime { get; init; }

        /// <summary>The children of <c>&lt;Users&gt;</c>.</summary>
        public string UsersXml { get; init; } = LastUserFirst;

        /// <summary>How long the box takes to answer a login request.</summary>
        public TimeSpan LoginDelay { get; init; }

        /// <summary>The answer to the n-th request that is not a login request (counting from 1).</summary>
        public Func<int, (HttpStatusCode Status, string Body)> Respond { get; init; } = _ => (HttpStatusCode.OK, Overview);

        public IReadOnlyList<Call> Calls
        {
            get
            {
                lock (gate)
                {
                    return [.. calls];
                }
            }
        }

        public IReadOnlyList<Call> LoginGets => [.. Calls.Where(c => c.IsLogin && c.Method == HttpMethod.Get)];

        public IReadOnlyList<Call> LoginPosts => [.. Calls.Where(c => c.IsLogin && c.Method == HttpMethod.Post)];

        public IReadOnlyList<Call> ApiCalls => [.. Calls.Where(c => !c.IsLogin)];

        /// <summary>The session the n-th successful login (counting from 1) gets.</summary>
        public static string Sid(int login) => (0x0123456789abcdefUL + (ulong)(login - 1)).ToString("x16");

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var call = new Call
            (
                request.Method,
                request.RequestUri!.PathAndQuery,
                request.Headers.TryGetValues("Authorization", out var authorization) ? string.Join(", ", authorization) : null,
                request.Content?.Headers.ContentType?.MediaType,
                request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken),
                Stopwatch.GetTimestamp()
            );

            int number;

            lock (gate)
            {
                calls.Add(call);
                number = calls.Count(c => !c.IsLogin);
            }

            if (call.IsLogin)
            {
                if (LoginDelay > TimeSpan.Zero)
                {
                    await Task.Delay(LoginDelay, cancellationToken);
                }

                return call.Method == HttpMethod.Get ? SessionInfo(NoSession) : Login(call);
            }

            if (!call.PathAndQuery.StartsWith("/api/v0/", StringComparison.Ordinal) && !call.PathAndQuery.StartsWith("/webservices/homeautoswitch.lua?", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(call.PathAndQuery);
            }

            var (status, body) = Respond(number);
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, call.PathAndQuery.StartsWith("/api/", StringComparison.Ordinal) ? "application/json" : "text/xml") };
        }

        private HttpResponseMessage Login(Call call)
        {
            var users = XElement.Parse($"<Users>{UsersXml}</Users>").Elements("User").Select(u => u.Value);

            var accepted = call.ContentType == "application/x-www-form-urlencoded" &&
                           users.Contains(call.Field("username")) &&
                           call.Field("response") == FritzBoxLogin.CreateResponse(Challenge, Password);

            return SessionInfo(accepted ? Sid(Interlocked.Increment(ref logins)) : NoSession);
        }

        private HttpResponseMessage SessionInfo(string sid) => new(HttpStatusCode.OK)
        {
            Content = new StringContent
            (
                $"""<?xml version="1.0" encoding="utf-8"?><SessionInfo><SID>{sid}</SID><Challenge>{Challenge}</Challenge><BlockTime>{BlockTime}</BlockTime><Rights></Rights><Users>{UsersXml}</Users></SessionInfo>""",
                Encoding.UTF8,
                "text/xml"
            ),
        };
    }
}
