using System.Net;
using System.Net.Http.Headers;
using System.Text;
using De.Hochstaetter.HomeAutomationClient.Services;
using De.Hochstaetter.HomeAutomationServer.Controllers;
using De.Hochstaetter.HomeAutomationServer.Misc;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using De.Hochstaetter.HomeAutomationServer.Models.Settings;
using De.Hochstaetter.HomeAutomationServer.Services;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

// Every call here goes to a loopback host and completes within a few milliseconds; a test that cannot be cancelled
// for that long costs nothing, and the tokens would only bury the assertions.
#pragma warning disable xUnit1051

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Hosted;

/// <summary>
/// Bearer tokens end to end: a Kestrel host with the real API authentication and <see cref="IdentityController"/>,
/// and the client's own <see cref="WebClientService"/> - how it gets a token, renews it, and gets a new one by
/// logging in again when the server has forgotten it, which a restart of the server is the real case of. Also that
/// Basic and cookie authentication are refused until <see cref="AuthenticationSettings"/> switches them on.
/// </summary>
public sealed class ApiAuthenticationTests : IAsyncLifetime
{
    private const string AdminName = "root";
    private const string OtherAdminName = "toor";
    private const string DeveloperName = "dev";
    private const string OpenApiDocument = "openapi/v1.json";

    private static readonly DateTimeOffset Now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly Settings settings = new() { Users = [TestUsers.Create(AdminName, Roles.Administrator), TestUsers.Create(OtherAdminName, Roles.Administrator), TestUsers.Create(DeveloperName, Roles.Developer)] };
    private readonly SettableTimeProvider serverClock = new(Now);
    private readonly ManualTimeProvider clientClock = new(Now);
    private readonly ILoggerFactory clientLogging = LoggerFactory.Create(builder => builder.AddTestOutput());

    private WebApplication app = null!;
    private string address = null!;
    private string apiUri = null!;
    private WebClientService client = null!;

    private AuthenticationSettings Authentication => settings.Authentication;

    public async ValueTask InitializeAsync()
    {
        app = await StartServer("http://127.0.0.1:0");
        address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        apiUri = address + "/api/";

        client = new WebClientService(clientLogging.CreateLogger<WebClientService>(), clientClock);
        client.Initialize(apiUri, "test", "1.0");
        Assert.Equal(HttpStatusCode.OK, (await client.Login(AdminName, TestUsers.Password)).Status);
    }

    public async ValueTask DisposeAsync()
    {
        client.Dispose();
        clientLogging.Dispose();
        await app.DisposeAsync();
    }

    [Fact]
    public async Task A_login_hands_out_a_bearer_token_that_the_API_accepts()
    {
        Assert.NotNull(client.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetUsers()).Status);

        using var http = Raw();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", client.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("Identity/users")).StatusCode);
    }

    [Fact]
    public void The_token_is_renewed_two_minutes_before_it_expires()
    {
        Assert.Equal(TimeSpan.FromMinutes(AuthenticationSettings.DefaultBearerTokenLifetimeMinutes) - WebClientService.RenewalLead, clientClock.Pending.DueTime);
    }

    [Fact]
    public async Task A_token_too_short_lived_for_that_is_renewed_half_way_through_its_life()
    {
        Authentication.BearerTokenLifetimeMinutes = 3;

        await client.Login(AdminName, TestUsers.Password);

        Assert.Equal(TimeSpan.FromSeconds(90), clientClock.Pending.DueTime);
    }

    [Fact]
    public async Task Renewing_swaps_the_token_for_a_new_one_and_schedules_the_next_renewal()
    {
        var before = client.AccessToken;
        var timer = clientClock.Pending;

        await client.RenewTokenAsync();

        Assert.NotNull(client.AccessToken);
        Assert.NotEqual(before, client.AccessToken);
        Assert.True(timer.IsDisposed);
        Assert.NotSame(timer, clientClock.Pending);
        Assert.Equal(HttpStatusCode.OK, (await client.GetUsers()).Status);
    }

    [Fact]
    public async Task A_restart_of_the_server_goes_unnoticed_by_the_caller()
    {
        var before = client.AccessToken;

        await RestartServer();

        Assert.Equal(HttpStatusCode.OK, (await client.GetUsers()).Status);
        Assert.NotEqual(before, client.AccessToken);
    }

    [Fact]
    public async Task A_restart_of_the_server_does_not_keep_the_client_off_the_hub()
    {
        await RestartServer();

        Assert.False(string.IsNullOrEmpty(await client.GetHubTicket()));
    }

    [Fact]
    public async Task Renewing_after_a_restart_of_the_server_logs_in_again()
    {
        var before = client.AccessToken;

        await RestartServer();
        await client.RenewTokenAsync();

        Assert.NotNull(client.AccessToken);
        Assert.NotEqual(before, client.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetUsers()).Status);
    }

    [Fact]
    public async Task A_token_that_ran_out_is_replaced_by_logging_in_again()
    {
        var before = client.AccessToken;

        serverClock.Now = Now + TimeSpan.FromMinutes(AuthenticationSettings.DefaultBearerTokenLifetimeMinutes);

        Assert.Equal(HttpStatusCode.OK, (await client.GetUsers()).Status);
        Assert.NotEqual(before, client.AccessToken);
    }

    [Fact]
    public async Task Renewing_while_the_server_cannot_be_reached_is_tried_again_later()
    {
        await app.StopAsync();

        await client.RenewTokenAsync();

        Assert.NotNull(client.AccessToken);
        Assert.Equal(WebClientService.RenewalRetryInterval, clientClock.Pending.DueTime);
    }

    [Fact]
    public async Task A_password_changed_elsewhere_ends_the_session_instead_of_being_retried_forever()
    {
        using var other = new WebClientService(null, new ManualTimeProvider(Now));
        other.Initialize(apiUri, "test", "1.0");
        await other.Login(OtherAdminName, TestUsers.Password);

        Assert.Equal(HttpStatusCode.OK, (await client.UpdateUser(OtherAdminName, new UserAccount { UserName = OtherAdminName, Password = "changed", Roles = Roles.Administrator })).Status);

        Assert.Equal(HttpStatusCode.Unauthorized, (await other.GetUsers()).Status);
        Assert.Null(other.AccessToken);
    }

    [Fact]
    public async Task A_login_the_server_refuses_ends_the_session_there_was()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.Login(AdminName, "wrong")).Status);

        Assert.Null(client.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetUsers()).Status);
    }

    [Fact]
    public async Task Logging_out_revokes_the_token_at_once()
    {
        var token = client.AccessToken;

        Assert.Equal(HttpStatusCode.OK, (await client.Logout()).Status);

        Assert.Null(client.AccessToken);
        Assert.True(clientClock.Timers.All(t => t.IsDisposed));
        using var http = Raw();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("Identity/users")).StatusCode);
    }

    [Fact]
    public async Task Basic_credentials_are_refused_until_they_are_switched_on()
    {
        using var http = Raw();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{AdminName}:{TestUsers.Password}")));

        using (var refused = await http.GetAsync("Identity/users"))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
            // No Basic challenge while Basic is off: a browser would answer it with a login box of its own.
            Assert.Equal(["Bearer"], refused.Headers.WwwAuthenticate.Select(h => h.Scheme));
        }

        Authentication.EnableBasicAuthentication = true;

        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("Identity/users")).StatusCode);

        http.DefaultRequestHeaders.Authorization = null;
        using var challenged = await http.GetAsync("Identity/users");
        Assert.Equal(["Bearer", "Basic"], challenged.Headers.WwwAuthenticate.Select(h => h.Scheme));
    }

    [Fact]
    public async Task A_malformed_Basic_header_is_refused_rather_than_failing_the_request()
    {
        Authentication.EnableBasicAuthentication = true;
        using var http = Raw();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "not base64!");

        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("Identity/users")).StatusCode);
    }

    [Fact]
    public async Task The_cookie_is_neither_set_nor_accepted_until_it_is_switched_on()
    {
        var cookies = new CookieContainer();
        using var http = Raw(cookies);
        var login = $"Identity/login?user={AdminName}&password={Uri.EscapeDataString(TestUsers.Password)}";

        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync(login)).StatusCode);
        Assert.Empty(cookies.GetAllCookies());
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("Identity/users")).StatusCode);

        Authentication.EnableCookieAuthentication = true;

        using (var response = await http.GetAsync(login))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var setCookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith(ApiAuthenticationService.AuthCookie + "=Bearer", StringComparison.Ordinal));
            Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains($"max-age={AuthenticationSettings.DefaultBearerTokenLifetimeMinutes * 60}", setCookie, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("Identity/users")).StatusCode);

        Authentication.EnableCookieAuthentication = false;

        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("Identity/users")).StatusCode);
    }

    private HttpClient Raw(CookieContainer? cookies = null, bool followRedirects = true) => new(new HttpClientHandler { CookieContainer = cookies ?? new CookieContainer(), AllowAutoRedirect = followRedirects })
    {
        BaseAddress = new Uri(apiUri),
    };

    [Fact]
    public async Task A_browser_tab_swaps_its_ticket_for_a_cookie_and_loses_it_from_the_address()
    {
        var address = await TabAddress(DeveloperName);
        Assert.Equal("/" + OpenApiDocument, address.AbsolutePath);
        Assert.Contains($"{BrowserTabTicket.QueryParameter}=", address.Query);

        using var http = Raw(followRedirects: false);
        using var redirect = await http.GetAsync(address);

        Assert.Equal(HttpStatusCode.Redirect, redirect.StatusCode);
        Assert.Equal("/" + OpenApiDocument, redirect.Headers.Location?.OriginalString);

        var setCookie = Assert.Single(redirect.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith(BrowserTabSessions.Cookie + "=", setCookie, StringComparison.Ordinal);
        Assert.Contains("path=/openapi", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
        // Although this host, like the real one behind its ingress, is reached over plain http.
        Assert.Contains("secure", setCookie.Split(';').Select(part => part.Trim()), StringComparer.OrdinalIgnoreCase);

        using var tab = await GetWithTabCookie(redirect.Headers.Location!.OriginalString, CookieValue(setCookie));
        Assert.Equal(HttpStatusCode.OK, tab.StatusCode);
        Assert.Equal("the document", await tab.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_browser_tab_ticket_works_once()
    {
        var address = await TabAddress(DeveloperName);

        Assert.NotNull(await OpenTab(address));
        Assert.Null(await OpenTab(address));
    }

    [Fact]
    public async Task A_browser_tab_ticket_expires_within_seconds()
    {
        var address = await TabAddress(DeveloperName);

        serverClock.Now = Now + BearerTokenService.TabTicketLifetime;

        Assert.Null(await OpenTab(address));
    }

    [Fact]
    public async Task A_browser_tab_of_somebody_who_is_no_developer_is_refused_the_document()
    {
        var cookie = Assert.IsType<string>(await OpenTab(await TabAddress(AdminName)));

        using var tab = await GetWithTabCookie("/" + OpenApiDocument, cookie);
        Assert.Equal(HttpStatusCode.Forbidden, tab.StatusCode);
    }

    [Fact]
    public async Task The_cookie_of_a_browser_tab_reaches_nothing_but_the_OpenAPI_document()
    {
        var cookie = Assert.IsType<string>(await OpenTab(await TabAddress(DeveloperName)));

        // A browser would not send it outside /openapi at all; the server must not take it there either.
        using var outside = await GetWithTabCookie("/api/Identity/hubTicket", cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, outside.StatusCode);
    }

    /// <summary>
    /// Opens the tab the way a browser does - the address with the ticket - and answers the cookie it was given, or
    /// <see langword="null"/> where it was given none. The redirect is not followed here: the cookie is
    /// <c>Secure</c>, and a cookie jar, a browser's as much as .NET's, does not send it back over the plain http this
    /// host is reached at. In production the browser speaks https to the ingress, so there it does.
    /// </summary>
    private async Task<string?> OpenTab(Uri address)
    {
        using var http = Raw(followRedirects: false);
        using var redirect = await http.GetAsync(address);

        Assert.Equal(HttpStatusCode.Redirect, redirect.StatusCode);

        return redirect.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.Where(value => value.StartsWith(BrowserTabSessions.Cookie + "=", StringComparison.Ordinal)).Select(CookieValue).SingleOrDefault()
            : null;
    }

    /// <summary>A request that carries the tab's cookie, as the browser sends it over https.</summary>
    private async Task<HttpResponseMessage> GetWithTabCookie(string path, string cookie)
    {
        using var http = Raw();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(address), path));
        request.Headers.Add("Cookie", $"{BrowserTabSessions.Cookie}={cookie}");
        return await http.SendAsync(request);
    }

    private static string CookieValue(string setCookie) => setCookie.Split(';')[0][(BrowserTabSessions.Cookie.Length + 1)..];

    /// <summary>The address of the OpenAPI document with a fresh ticket, as the menu item gets it.</summary>
    private async Task<Uri> TabAddress(string userName)
    {
        using var user = new WebClientService(null, new ManualTimeProvider(Now));
        user.Initialize(apiUri, "test", "1.0");
        Assert.Equal(HttpStatusCode.OK, (await user.Login(userName, TestUsers.Password)).Status);

        var result = await user.GetBrowserTabUri(OpenApiDocument);

        Assert.Equal(HttpStatusCode.OK, result.Status);
        return Assert.IsType<Uri>(result.Payload);
    }

    /// <summary>
    /// Stops the server and starts a new one at the same address with the same users, the way a restart reads them
    /// back from Settings.xml. Only the tokens are gone, because nothing but the memory of the old server held them.
    /// </summary>
    private async Task RestartServer()
    {
        await app.DisposeAsync();
        app = await StartServer(address);
    }

    private async Task<WebApplication> StartServer(string url)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.AddTestOutput(LogLevel.Information);
        builder.WebHost.UseUrls(url);

        // The very wiring Program.cs has: the settings own the users and the authentication settings, and the
        // options hand the same objects on, so a test that changes a setting changes what the server does.
        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton<IAesKeyProvider>(new TestAesKeyProvider());
        builder.Services.Configure<UserList>(u =>
        {
            u.Users = settings.Users;
            u.Authentication = settings.Authentication;
        });

        // Registered before AddApiAuthentication, which only adds TimeProvider.System if nobody else has.
        builder.Services.AddSingleton<TimeProvider>(serverClock);
        builder.Services.AddControllers().AddApplicationPart(typeof(IdentityController).Assembly);
        builder.Services.AddApiAuthentication();
        builder.Services.AddHubTicketAuthentication();

        var server = builder.Build();

        // In the order Program.cs has them: the ticket is swapped before anything is authenticated.
        server.UseBrowserTabSessions();
        server.UseAuthentication();
        server.UseAuthorization();

        server.MapControllers();

        // A stand-in for MapOpenApi, under the policy Program.cs gives it.
        server.MapGet("/" + OpenApiDocument, () => "the document")
            .RequireAuthorization(policy => policy.AddAuthenticationSchemes(ApiAuthenticationService.SchemeName).RequireRole(nameof(Roles.Developer)));
        await server.StartAsync();
        return server;
    }
}
