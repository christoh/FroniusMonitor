using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace De.Hochstaetter.HomeAutomationServer.Misc;

/// <summary>
/// The fallback that serves the browser client for a path of its own, in one place so that the tests check the
/// routes the server actually maps.
/// </summary>
public static class BrowserClientHosting
{
    /// <summary>The first path segments that belong to the server and never to the client.</summary>
    public static readonly IReadOnlyList<string> ServerSegments = ["api", "hub"];

    /// <summary>
    /// Maps the lowest-priority endpoints: only a request that matched nothing else lands here. A full page load or
    /// reload of e.g. <c>/inverterdetails/Fronius/1234</c> still gets the client's <c>index.html</c> instead of a
    /// 404; no effect where wwwroot has no client published into it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A path below one of the <see cref="ServerSegments"/> is never the client's, so it gets a 404 instead of the
    /// client. Handing out <c>index.html</c> there made a mistyped API call look like a success with an HTML body,
    /// which a caller expecting JSON only notices when it fails to parse it.
    /// </para>
    /// <para>
    /// Those are fallbacks as well, not ordinary endpoints, so every endpoint that does exist below them still wins.
    /// Among the fallbacks, <c>api/{**path}</c> is more specific than the client's catch-all and wins over it. It
    /// takes any method, so a known endpoint called with the wrong one is a 404 too, not a 405 - just as it used to
    /// be the client rather than a 405.
    /// </para>
    /// </remarks>
    public static void MapBrowserClientFallback(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapFallbackToFile("index.html");

        foreach (var segment in ServerSegments)
        {
            endpoints.MapFallback($"/{segment}/{{**path}}", () => Results.NotFound());
        }
    }
}
