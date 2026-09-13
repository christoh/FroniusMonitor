namespace De.Hochstaetter.Fronius.Validators;

/// <summary>
/// A complete address, as a user would type it into the address bar of a browser:
/// <c>https://home.example.com</c>. Anything without a scheme or without a host is refused, and so is a scheme
/// this rule was not given - <see cref="Schemes"/> is http and https unless something says otherwise.
/// </summary>
/// <remarks>
/// <see cref="Parse"/> is the one place that decides what a valid address is, and it is public because a caller
/// almost always needs the <see cref="Uri"/> as well as the verdict. Anything that builds an address on top of
/// what the user typed has to go through it, so that what a rule lets through and what the code then uses can
/// never drift apart.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class AbsoluteUriAttribute : ValidationRuleAttribute
{
    private static readonly string[] defaultSchemes = [Uri.UriSchemeHttps, Uri.UriSchemeHttp];

    /// <summary>The schemes taken, compared case insensitively. Http and https by default.</summary>
    public string[] Schemes { get; set; } = defaultSchemes;

    protected override string Complain(object? value) => Resources.MustBeValidUrl;

    protected override bool IsAcceptable(object? value) => Parse(value?.ToString(), Schemes) != null;

    /// <summary>
    /// The address <paramref name="text"/> holds, or <see langword="null"/> where it is not one this rule accepts.
    /// </summary>
    /// <param name="text">What the user typed. Surrounding white space is theirs to leave in and ours to ignore.</param>
    /// <param name="schemes">The schemes to take. Http and https where nothing is passed.</param>
    public static Uri? Parse(string? text, IEnumerable<string>? schemes = null)
    {
        if (string.IsNullOrWhiteSpace(text) || !Uri.TryCreate(text.Trim(), UriKind.Absolute, out var uri))
        {
            return null;
        }

        // Uri takes "https:///nowhere" and leaves the host empty, which is not an address anything can be
        // reached at.
        if (Uri.CheckHostName(uri.Host) is UriHostNameType.Unknown)
        {
            return null;
        }

        return (schemes ?? defaultSchemes).Any(scheme => string.Equals(scheme, uri.Scheme, StringComparison.OrdinalIgnoreCase)) ? uri : null;
    }
}
