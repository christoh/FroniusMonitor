using System.Net;
using De.Hochstaetter.HomeAutomationServer.Models.Settings;
using De.Hochstaetter.HomeAutomationServer.Models.SolarWeb;
using De.Hochstaetter.HomeAutomationServer.Services.SolarWeb;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Hosted;

/// <summary>
/// The real <see cref="SolarWebClient"/> against a Kestrel host that plays Solar.web and the Fronius login at once:
/// the redirect chain to the login form, the form post to <c>/commonauth</c>, the page that posts the code back,
/// the session cookie, and the ways the real service refuses. What is under test is the client's handling of the
/// pages, with the redirects and cookies done by the very <see cref="HttpClientHandler"/> the server runs with.
/// </summary>
public sealed class SolarWebLoginTests : IAsyncLifetime
{
    private const string UserName = "you@example.com";
    private const string Password = "correct horse battery staple";
    private const string PvSystemId = "318e321a-883b-4a82-868a-0fdc784476ad";
    private const string SessionDataKey = "908058f4-200b-4c97-89c8-3719e69f743c";
    private const string Code = "0b2a4c3c-1d0d-3e71-9c2a-3d1f9c5c9a11";

    private const string ChartJson = """{"isPremiumFeature":false,"settings":{"series":[{"type":"column","name":"Energie ins Netz eingespeist","data":[[1788220800000,57.89]],"yAxis":"kWh","id":"FromGenToGrid"}]},"title":"September 2026","sumValue":"57,89 kWh"}""";

    private readonly SettableTimeProvider clock = new(new DateTimeOffset(2026, 9, 20, 10, 0, 0, TimeSpan.Zero));
    private WebApplication app = null!;
    private SolarWebSettings settings = null!;
    private SolarWebClient client = null!;

    private int chartRequests;
    private int loginPosts;
    private int callbackPosts;
    private bool rateLimited;
    private bool broken;
    private bool down;
    private bool maintenance;
    private bool blocked;
    private string? lastLoginBody;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.AddTestOutput(LogLevel.Warning);
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        app = builder.Build();

        // Solar.web: the chart, behind its session cookie.
        app.MapGet("/Chart/GetChartNew", (HttpContext context) =>
        {
            chartRequests++;

            if (rateLimited)
            {
                context.Response.Headers.RetryAfter = "7";
                return Results.StatusCode(StatusCodes.Status429TooManyRequests);
            }

            if (broken)
            {
                return Results.Content("<html><body>500 - Internal server error.</body></html>", "text/html", statusCode: StatusCodes.Status500InternalServerError);
            }

            if (down)
            {
                context.Response.Headers.RetryAfter = "120";
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            if (maintenance)
            {
                // Fronius' page of Sunday 2026-09-20, as a browser saw it: 200, HTML, no form.
                return Results.Content("<html><head><title>Fronius</title><style>h1{font-size:3em}</style></head><body><img src=\"/logo.svg\"><h1>Maintenance Work</h1><p>We are currently optimizing our service for you so you can find what you need even faster in the future. This page is therefore temporarily unavailable. We will be back online soon. Thank you for your understanding!</p><h1>Wartungsarbeiten</h1><p>Wir optimieren gerade unseren Service für Sie.</p></body></html>", "text/html");
            }

            if (blocked)
            {
                // What a web application firewall's block page looks like: 200, HTML, no form.
                return Results.Content("<html><head><title>Request Rejected</title><style>body{color:red}</style></head><body><script>var x=1;</script>The requested URL was rejected. Please consult with your administrator.<br><br>Your support ID is: 4711</body></html>", "text/html");
            }

            if (context.Request.Cookies["SolarWebSession"] != "yes")
            {
                return Results.Redirect($"/Account/ExternalLogin?ReturnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}");
            }

            return Results.Content(ChartJson, "application/json; charset=utf-8");
        });

        app.MapGet("/Firmware/GetComponentUpdateInfos", (HttpContext context) =>
        {
            if (context.Request.Cookies["SolarWebSession"] != "yes")
            {
                return Results.Redirect($"/Account/ExternalLogin?ReturnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}");
            }

            return Results.Content(SolarWebFirmwareTests.Answer, "application/json; charset=utf-8");
        });

        app.MapGet("/Account/ExternalLogin", (HttpContext context) =>
        {
            // The real one keeps the return url in the encrypted state; a cookie does here.
            context.Response.Cookies.Append("ReturnUrl", Uri.EscapeDataString(context.Request.Query["ReturnUrl"].ToString()));
            return Results.Redirect("/oauth2/authorize?client_id=mf_o9iTAyKemNLQTa6Sp6HYonCIa&response_type=code%20id_token&response_mode=form_post&state=STATE");
        });

        // login.fronius.com: the authorization endpoint, the login form, the credential check.
        app.MapGet("/oauth2/authorize", (HttpContext context) =>
        {
            if (context.Request.Cookies["commonAuthId"] != "sso")
            {
                return Results.Redirect($"/authenticationendpoint/login.do?client_id=mf_o9iTAyKemNLQTa6Sp6HYonCIa&sessionDataKey={SessionDataKey}&relyingParty=mf_o9iTAyKemNLQTa6Sp6HYonCIa");
            }

            return Results.Content($"""
                <html><body onload="javascript:document.forms[0].submit()">
                <form method="post" action="/Account/ExternalLoginCallback">
                    <input type="hidden" name="code" value="{Code}"/>
                    <input type="hidden" name="id_token" value="eyJ.eyJ.c2ln"/>
                    <input type="hidden" name="state" value="{context.Request.Query["state"]}"/>
                    <button type="submit">POST</button>
                </form></body></html>
                """, "text/html");
        });

        app.MapGet("/authenticationendpoint/login.do", (HttpContext context) => Results.Content($"""
            <html><body>
            <form class="reset-input fro-form" action="../commonauth" method="post" id="loginForm">
                <input type="hidden" name="authenticators" value="SAMLSSOAuthenticator:Fronius Login;FroniusBasicAuthenticator:LOCAL">
                <input type="hidden" name="tenantDomain" value="carbon.super">
                {(context.Request.Query.ContainsKey("authFailure") ? "<div id=\"error-msg\">Login failed! Please recheck the username and password and try again.</div>" : "")}
                <input type="text" name="usernameUserInput" id="usernameUserInput" value="" />
                <input id="username" name="username" type="hidden" value="null">
                <input type="password" id="password" name="password" value="" autocomplete="off" />
                <input class="fro-checkbox" type="checkbox" id="chkRemember" name="chkRemember">
                <input type="hidden" name="sessionDataKey" value='{context.Request.Query["sessionDataKey"]}'/>
                <button id="login-button" type="submit">Login</button>
            </form></body></html>
            """, "text/html"));

        app.MapPost("/commonauth", async (HttpContext context) =>
        {
            loginPosts++;
            var form = await context.Request.ReadFormAsync();
            lastLoginBody = string.Join("&", form.Select(f => $"{f.Key}={f.Value}"));

            if (form["sessionDataKey"] != SessionDataKey || form["username"] != UserName || form["password"] != Password)
            {
                return Results.Redirect($"/authenticationendpoint/login.do?sessionDataKey={SessionDataKey}&authFailure=true&authFailureMsg=login.fail.message");
            }

            context.Response.Cookies.Append("commonAuthId", "sso");
            return Results.Redirect("/oauth2/authorize?sessionDataKey=" + SessionDataKey + "&state=STATE");
        });

        // Solar.web again: the code comes back and becomes the session.
        app.MapPost("/Account/ExternalLoginCallback", async (HttpContext context) =>
        {
            callbackPosts++;
            var form = await context.Request.ReadFormAsync();

            if (form["code"] != Code || form["state"] != "STATE")
            {
                return Results.StatusCode(StatusCodes.Status400BadRequest);
            }

            context.Response.Cookies.Append("SolarWebSession", "yes");
            return Results.Redirect(Uri.UnescapeDataString(context.Request.Cookies["ReturnUrl"] ?? "/"));
        });

        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();

        settings = new SolarWebSettings { BaseUrl = address, UserName = UserName, Password = Password, PvSystemId = PvSystemId };
        client = new SolarWebClient(NullLogger<SolarWebClient>.Instance, SolarWebClient.CreateHandler(), clock);
    }

    public async ValueTask DisposeAsync()
    {
        client.Dispose();
        await app.DisposeAsync();
    }

    [Fact]
    public async Task The_first_chart_logs_in_through_the_whole_chain_and_the_second_uses_the_session()
    {
        var chart = await client.GetChartAsync(settings, SolarWebInterval.Month, SolarWebView.Production, new DateOnly(2026, 9, 20), TestContext.Current.CancellationToken);

        Assert.Equal(1, loginPosts);
        Assert.Equal(1, callbackPosts);
        Assert.Equal("September 2026", chart.Title);
        Assert.Equal("57,89 kWh", chart.SumValue);
        Assert.Equal(new DateOnly(2026, 9, 1), chart.Period);
        Assert.Equal(clock.Now.UtcDateTime, chart.FetchedUtc);
        Assert.Equal(57.89, Assert.Single(Assert.Single(chart.Series).Points).Value);

        // What /commonauth saw: the hidden fields as they were, the user name in both fields, the password, the box ticked.
        Assert.NotNull(lastLoginBody);
        Assert.Contains($"username={UserName}", lastLoginBody);
        Assert.Contains($"usernameUserInput={UserName}", lastLoginBody);
        Assert.Contains($"password={Password}", lastLoginBody);
        Assert.Contains("chkRemember=on", lastLoginBody);
        Assert.Contains("tenantDomain=carbon.super", lastLoginBody);
        Assert.DoesNotContain("username=null", lastLoginBody);

        var before = chartRequests;
        await client.GetChartAsync(settings, SolarWebInterval.Day, SolarWebView.Consumption, new DateOnly(2026, 9, 19), TestContext.Current.CancellationToken);

        Assert.Equal(1, loginPosts);
        Assert.Equal(1, callbackPosts);
        Assert.Equal(before + 1, chartRequests);
    }

    [Fact]
    public async Task A_rejected_password_is_thrown_after_one_attempt_and_no_password_is_not_even_sent()
    {
        settings.Password = "wrong";

        var ex = await Assert.ThrowsAsync<SolarWebLoginException>(() => client.GetChartAsync(settings, SolarWebInterval.Month, SolarWebView.Production, new DateOnly(2026, 9, 20), TestContext.Current.CancellationToken));

        Assert.Contains(UserName, ex.Message);
        Assert.Equal(1, loginPosts);
        Assert.Equal(0, callbackPosts);

        settings.Password = string.Empty;
        await Assert.ThrowsAsync<SolarWebLoginException>(() => client.GetChartAsync(settings, SolarWebInterval.Month, SolarWebView.Production, new DateOnly(2026, 9, 20), TestContext.Current.CancellationToken));
        Assert.Equal(1, loginPosts);
    }

    [Fact]
    public async Task A_429_is_a_rate_limit_exception_with_the_retry_after_and_a_500_is_a_request_exception()
    {
        rateLimited = true;
        var limited = await Assert.ThrowsAsync<SolarWebRateLimitException>(() => client.GetChartAsync(settings, SolarWebInterval.Month, SolarWebView.Production, new DateOnly(2026, 9, 20), TestContext.Current.CancellationToken));
        Assert.Equal(TimeSpan.FromSeconds(7), limited.RetryAfter);
        Assert.Equal(0, loginPosts);

        rateLimited = false;
        broken = true;
        var failed = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetChartAsync(settings, SolarWebInterval.Month, SolarWebView.Production, new DateOnly(2026, 9, 20), TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
    }

    [Fact]
    public async Task A_503_and_the_maintenance_page_mean_unavailable_and_any_other_html_page_is_a_transient_failure_that_says_what_the_page_was()
    {
        down = true;
        var unavailable = await Assert.ThrowsAsync<SolarWebUnavailableException>(() => client.GetChartAsync(settings, SolarWebInterval.Month, SolarWebView.Production, new DateOnly(2026, 9, 20), TestContext.Current.CancellationToken));
        Assert.Equal(TimeSpan.FromSeconds(120), unavailable.RetryAfter);
        Assert.Contains("503", unavailable.Message);

        down = false;
        maintenance = true;
        var maintained = await Assert.ThrowsAsync<SolarWebUnavailableException>(() => client.GetChartAsync(settings, SolarWebInterval.Month, SolarWebView.Production, new DateOnly(2026, 9, 20), TestContext.Current.CancellationToken));
        Assert.Null(maintained.RetryAfter);
        Assert.Contains("200 text/html", maintained.Message);
        Assert.Contains("'Fronius': Maintenance Work We are currently optimizing our service", maintained.Message);
        Assert.DoesNotContain("font-size", maintained.Message);

        maintenance = false;
        blocked = true;
        var rejected = await Assert.ThrowsAsync<InvalidDataException>(() => client.GetChartAsync(settings, SolarWebInterval.Month, SolarWebView.Production, new DateOnly(2026, 9, 20), TestContext.Current.CancellationToken));
        Assert.Contains("'Request Rejected': The requested URL was rejected. Please consult with your administrator. Your support ID is: 4711", rejected.Message);
        Assert.DoesNotContain("var x", rejected.Message);
        Assert.Equal(0, loginPosts);

        // None of that was a login failure: the next request logs in as if nothing had happened.
        blocked = false;
        await client.GetChartAsync(settings, SolarWebInterval.Month, SolarWebView.Production, new DateOnly(2026, 9, 20), TestContext.Current.CancellationToken);
        Assert.Equal(1, loginPosts);
    }

    [Fact]
    public async Task The_firmware_status_logs_in_the_same_way_and_comes_back_parsed()
    {
        var status = await client.GetFirmwareStatusAsync(settings, TestContext.Current.CancellationToken);

        Assert.Equal(1, loginPosts);
        Assert.Equal(PvSystemId, status.PvSystemId);
        Assert.Equal(3, status.Components.Count);
        Assert.True(status.HasOutdatedFirmware);
        Assert.Equal(new Version(1, 41, 11, 1), status.Components[1].InstalledVersion);
    }

    [Fact]
    public void The_addresses_are_the_ones_the_solar_web_pages_request()
    {
        var settings = new SolarWebSettings { PvSystemId = PvSystemId };
        var uri = SolarWebClient.ChartUri(settings, SolarWebInterval.Year, SolarWebView.ReturnOfInvestment, new DateOnly(2026, 9, 20));
        Assert.Equal($"https://www.solarweb.com/Chart/GetChartNew?pvSystemId={PvSystemId}&year=2026&month=9&day=20&interval=year&view=returnofinvestment", uri.ToString());
        Assert.Equal($"https://www.solarweb.com/Firmware/GetComponentUpdateInfos?pvSystemId={PvSystemId}", SolarWebClient.FirmwareUri(settings).ToString());
    }
}
