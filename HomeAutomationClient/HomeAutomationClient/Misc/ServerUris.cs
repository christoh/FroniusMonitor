namespace De.Hochstaetter.HomeAutomationClient.Misc;

/// <summary>
/// The two addresses the client talks to the server with, derived from the one address a user knows: the root the
/// server is reached at. Everything that needs them - the browser head at startup, the login dialog when the user
/// changes the connection - goes through here, so the arithmetic exists once and the heads cannot drift apart.
/// </summary>
public static class ServerUris
{
    private const string ApiSegment = "api/";
    private const string HubSegment = "hub";

    /// <summary>
    /// The api and hub addresses below <paramref name="root"/>.
    /// </summary>
    /// <param name="root">
    /// The root the server is reached at. A trailing slash is added where it is missing: without it Uri resolves
    /// the segments against the parent of the last one, so https://example.com/home would yield
    /// https://example.com/api/.
    /// </param>
    public static (string ApiUri, string HubUri) From(Uri root)
    {
        var withSlash = root.AbsoluteUri.EndsWith('/') ? root : new Uri(root.AbsoluteUri + "/");
        return (new Uri(withSlash, ApiSegment).ToString(), new Uri(withSlash, HubSegment).ToString());
    }

    /// <summary>
    /// The api and hub addresses below <paramref name="root"/>, or <see langword="null"/> where
    /// <paramref name="root"/> is not an address the client can reach a server at.
    /// </summary>
    public static (string ApiUri, string HubUri)? From(string? root) => AbsoluteUriAttribute.Parse(root) is { } uri ? From(uri) : null;

    /// <summary>
    /// The root <paramref name="apiUri"/> sits below - what <see cref="From(Uri)"/> was given - or
    /// <see langword="null"/> where <paramref name="apiUri"/> is not an address of ours.
    /// </summary>
    public static string? RootOf(string? apiUri)
    {
        if (AbsoluteUriAttribute.Parse(apiUri) is not { } uri)
        {
            return null;
        }

        // "../" against .../api/ gives the parent; the trailing slash of api/ is what makes that the root and not
        // the parent of the root.
        var root = new Uri(uri, "../");

        // What the user typed had no trailing slash unless they put one there, and showing them one they did not
        // type makes the box look like it changed behind their back.
        return root.AbsoluteUri.TrimEnd('/');
    }

    /// <summary>
    /// Whether both addresses are ones the client can work with. A cache holding anything else - nothing at all on
    /// a fresh phone, or a leftover from a server that has moved - means the user has to be asked again.
    /// </summary>
    public static bool AreUsable(string? apiUri, string? hubUri) => AbsoluteUriAttribute.Parse(apiUri) != null && AbsoluteUriAttribute.Parse(hubUri) != null;
}
