using System.Reflection;
using De.Hochstaetter.Fronius.Models.Settings;
using De.Hochstaetter.Fronius.Services;
using De.Hochstaetter.HomeAutomationServerTests.Logging;
using Microsoft.Extensions.Logging;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// What assigning <see cref="Gen24Service.Connection"/> does to the HTTP client behind it.
/// </summary>
/// <remarks>
/// <para>
/// It used to throw the client away every time, and the property is assigned far more often than the inverter
/// changes: <c>Gen24DataCollector</c> assigns a fresh clone of the same connection on every round of its sensor
/// loop, and a controller assigns the connection it looked up on every request. The config read of the collector
/// runs beside that sensor loop on the same service, so the client went out from under it and the read failed
/// with <c>ObjectDisposedException: De.Hochstaetter.Fronius.Services.DigestAuthHttp</c>.
/// </para>
/// <para>
/// The client is private, and everything that would show it is behind a call to a real inverter. So the field is
/// read by reflection: it is nulled whenever it is disposed, which makes "the same instance is still there" the
/// same statement as "it was not disposed".
/// </para>
/// </remarks>
public class Gen24ServiceConnectionTests
{
    // Nothing listens here, so the first call fails at once instead of waiting out the 30 second timeout of the
    // client - which is all this needs, because the client is built before the call goes out.
    private const string Nowhere = "http://127.0.0.1:1/";

    private static WebConnection Connection(string baseUrl = Nowhere, string userName = "customer", string password = "secret") =>
        new() { BaseUrl = baseUrl, UserName = userName, Password = password };

    private static Gen24Service NewService() => new(new Gen24JsonService(), new Logger<Gen24Service>(new LoggerFactory([new TestOutputLoggerProvider()])));

    private static DigestAuthHttp? ClientOf(Gen24Service service) =>
        (DigestAuthHttp?)typeof(Gen24Service)
            .GetField("froniusHttpClient", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(service);

    /// <summary>Builds the client the way the first request to an inverter would, without needing one.</summary>
    private static async Task<DigestAuthHttp> ForceClient(Gen24Service service)
    {
        await Assert.ThrowsAnyAsync<Exception>(() => service.GetFroniusJsonResponse("api/status/version").AsTask());

        var client = ClientOf(service);
        Assert.NotNull(client);
        return client;
    }

    [Fact]
    public async Task Assigning_the_same_inverter_again_keeps_the_client()
    {
        var service = NewService();
        service.Connection = Connection();
        var client = await ForceClient(service);

        // What the collector does on every round of its sensor loop.
        service.Connection = (WebConnection)service.Connection.Clone();

        Assert.Same(client, ClientOf(service));
    }

    [Fact]
    public async Task Assigning_an_equal_connection_that_is_not_a_clone_keeps_the_client_too()
    {
        // What a controller does on every request: the connection it looked up, a different object with the same
        // contents.
        var service = NewService();
        service.Connection = Connection();
        var client = await ForceClient(service);

        service.Connection = Connection();

        Assert.Same(client, ClientOf(service));
    }

    [Theory]
    [InlineData("http://192.168.44.11/", "customer", "secret")]
    [InlineData(Nowhere, "operator", "secret")]
    [InlineData(Nowhere, "customer", "different")]
    public async Task Assigning_another_inverter_throws_the_client_away(string baseUrl, string userName, string password)
    {
        var service = NewService();
        service.Connection = Connection();
        await ForceClient(service);

        service.Connection = Connection(baseUrl, userName, password);

        Assert.Null(ClientOf(service));
    }

    [Fact]
    public async Task Clearing_the_connection_throws_the_client_away()
    {
        var service = NewService();
        service.Connection = Connection();
        await ForceClient(service);

        service.Connection = null;

        Assert.Null(ClientOf(service));
    }

    [Fact]
    public async Task The_address_is_compared_without_regard_to_case()
    {
        var service = NewService();
        service.Connection = Connection();
        var client = await ForceClient(service);

        service.Connection = Connection(Nowhere.ToUpperInvariant());

        Assert.Same(client, ClientOf(service));
    }
}
