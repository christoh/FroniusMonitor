using System.Net;
using De.Hochstaetter.HomeAutomationServer.Misc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

// Every call here goes to a loopback host and completes within a few milliseconds; a test that cannot be cancelled
// for that long costs nothing, and the tokens would only bury the assertions.
#pragma warning disable xUnit1051

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Hosted;

/// <summary>
/// The fallback that hands the browser client its <c>index.html</c> for a deep link, over a real Kestrel host with a
/// wwwroot of its own: the client's paths get the page, and anything below <c>/api</c> or <c>/hub</c> that does not
/// exist gets a 404 rather than the page.
/// </summary>
public sealed class BrowserClientFallbackTests : IAsyncLifetime
{
    private const string IndexHtml = "<!DOCTYPE html><html><body>client</body></html>";

    private readonly string webRoot = Path.Combine(Path.GetTempPath(), $"BrowserClientFallbackTests-{Guid.NewGuid():N}");

    private WebApplication app = null!;
    private HttpClient http = null!;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(webRoot);
        await File.WriteAllTextAsync(Path.Combine(webRoot, "index.html"), IndexHtml, TestContext.Current.CancellationToken);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { WebRootPath = webRoot });
        builder.Logging.AddTestOutput(LogLevel.Warning);
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        app = builder.Build();

        // Stand-ins for a controller and the hub: an endpoint that exists, one that only takes a POST.
        app.MapGet("/api/Devices", () => "devices");
        app.MapPost("/api/Identity/token", () => "token");
        app.MapGet("/hub/negotiate", () => "negotiate");

        // The very fallback Program.cs maps.
        app.MapBrowserClientFallback();

        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        http = new HttpClient { BaseAddress = new Uri(address) };
    }

    public async ValueTask DisposeAsync()
    {
        http.Dispose();
        await app.DisposeAsync();
        Directory.Delete(webRoot, recursive: true);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/inverterdetails/Fronius/1234")]
    [InlineData("/powerflow")]
    // Only the whole segment is reserved: a client path that merely starts with the letters is the client's.
    [InlineData("/apiary")]
    [InlineData("/hubcap/1")]
    public async Task A_path_of_the_client_gets_the_client(string path)
    {
        using var response = await http.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(IndexHtml, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/api")]
    [InlineData("/api/")]
    [InlineData("/api/DoesNotExist")]
    [InlineData("/api/Devices/nothing/here")]
    [InlineData("/API/DoesNotExist")]
    [InlineData("/hub")]
    [InlineData("/hub/DoesNotExist")]
    [InlineData("/Hub/DoesNotExist")]
    public async Task A_path_below_api_or_hub_that_does_not_exist_is_a_404(string path)
    {
        using var response = await http.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual(IndexHtml, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_path_below_api_that_does_exist_is_answered_by_its_endpoint()
    {
        Assert.Equal("devices", await http.GetStringAsync("/api/Devices"));
        Assert.Equal("negotiate", await http.GetStringAsync("/hub/negotiate"));
    }

    [Fact]
    public async Task An_API_endpoint_called_with_the_wrong_method_is_a_404_not_the_client()
    {
        using var response = await http.GetAsync("/api/Identity/token");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var post = await http.PostAsync("/api/Identity/token", null);
        Assert.Equal("token", await post.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_missing_file_is_a_404_not_the_client()
    {
        using var response = await http.GetAsync("/_framework/missing.js");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
