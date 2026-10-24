using System.Net;
using System.Runtime.InteropServices;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.HomeAutomationClient.Services;
using De.Hochstaetter.HomeAutomationServer.Controllers;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using De.Hochstaetter.HomeAutomationServer.Models.Settings;
using De.Hochstaetter.HomeAutomationServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

// Every call here goes to a loopback host and completes within a few milliseconds; a test that cannot be cancelled
// for that long costs nothing, and the tokens would only bury the assertions.
#pragma warning disable xUnit1051

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Hosted;

/// <summary>
/// <c>GET /api</c> of <see cref="ApiController"/>, end to end: a Kestrel host with the real API authentication, and
/// the client's own <see cref="WebClientService"/> asking it without having logged in, the way the About dialog does
/// - so the address, the anonymous access and the JSON shape of <see cref="WebApiInfo"/> are proven on both sides.
/// </summary>
public sealed class ServerInfoTests : IAsyncLifetime
{
    private readonly Settings settings = new();

    private WebApplication app = null!;
    private WebClientService client = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.AddTestOutput(LogLevel.Warning);
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(settings);
        builder.Services.Configure<UserList>(u => u.Users = settings.Users);
        builder.Services.AddControllers().AddApplicationPart(typeof(ApiController).Assembly);
        builder.Services.AddApiAuthentication();

        app = builder.Build();
        app.MapControllers();
        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        client = new WebClientService();
        client.Initialize(address + "/api/", "test", "1.0");
    }

    public async ValueTask DisposeAsync()
    {
        client.Dispose();
        await app.DisposeAsync();
    }

    [Fact]
    public async Task The_server_tells_anybody_its_version_build_and_platform()
    {
        var result = await client.GetServerInfo();

        Assert.Equal(HttpStatusCode.OK, result.Status);
        var info = Assert.IsType<WebApiInfo>(result.Payload);
        Assert.Equal($"Home Automation Server {RuntimeInformation.ProcessArchitecture}", info.ProductName);
        Assert.Equal(FroniusGitInfo.VersionString, info.Version);
        Assert.Equal(RuntimeInformation.OSDescription, info.OsVersion);
        Assert.Equal($"{RuntimeInformation.OSArchitecture}", info.OsArch);
        Assert.Equal(RuntimeInformation.FrameworkDescription, info.DotNetVersion);
        Assert.Equal(FroniusGitInfo.Branch, info.Branch);
        Assert.Equal(FroniusGitInfo.CommitId, info.CommitId);
        Assert.Equal(FroniusGitInfo.IsDirty, info.IsDirty);
        Assert.Equal(FroniusGitInfo.Configuration, info.Configuration);
    }

    [Fact]
    public async Task The_commit_time_arrives_as_the_same_moment_in_UTC()
    {
        var info = (await client.GetServerInfo()).Payload!;

        Assert.Equal(FroniusGitInfo.CommitTime, info.CommitTime);
        Assert.Equal(TimeSpan.Zero, info.CommitTime?.Offset);
    }
}
