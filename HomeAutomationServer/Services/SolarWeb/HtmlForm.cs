namespace De.Hochstaetter.HomeAutomationServer.Services.SolarWeb;

/// <summary>
///     A <c>&lt;form&gt;</c> of an HTML page, as much of it as a login flow needs: where it posts to and the fields
///     it would send. Enough for the two pages the Fronius login consists of - the login form and the page that
///     posts the authorization code back to Solar.web - without an HTML parser package for two pages.
/// </summary>
/// <remarks>
///     What a browser would send is what is collected: every <c>input</c> with a name, except buttons and unchecked
///     check boxes. Attribute order does not matter, both quote styles are read, and entities are decoded.
/// </remarks>
public sealed partial class HtmlForm
{
    /// <summary>The <c>action</c> attribute as written, which may be relative to the page. <see langword="null" /> where the form has none.</summary>
    public string? Action { get; init; }

    /// <summary>The fields in document order. A name that occurs twice occurs twice here as well.</summary>
    public List<KeyValuePair<string, string>> Fields { get; init; } = [];

    public bool Has(string name) => Fields.Any(f => f.Key == name);

    public string? this[string name] => Fields.FirstOrDefault(f => f.Key == name).Value;

    /// <summary>Every form on the page, in document order.</summary>
    public static IReadOnlyList<HtmlForm> Parse(string html)
    {
        var result = new List<HtmlForm>();

        foreach (Match form in FormRegex().Matches(html))
        {
            var attributes = Attributes(form.Groups["open"].Value);
            var fields = new List<KeyValuePair<string, string>>();

            foreach (Match input in InputRegex().Matches(form.Groups["body"].Value))
            {
                var a = Attributes(input.Value);

                if (!a.TryGetValue("name", out var name) || name.Length == 0)
                {
                    continue;
                }

                var type = a.GetValueOrDefault("type", "text").ToLowerInvariant();

                if (type is "submit" or "button" or "reset" or "image" || type is "checkbox" or "radio" && !a.ContainsKey("checked"))
                {
                    continue;
                }

                // A checked box without a value sends "on", like a browser.
                fields.Add(new KeyValuePair<string, string>(name, a.GetValueOrDefault("value", type is "checkbox" or "radio" ? "on" : string.Empty)));
            }

            result.Add(new HtmlForm { Action = attributes.GetValueOrDefault("action"), Fields = fields });
        }

        return result;
    }

    /// <summary>The fields as a request body, with <paramref name="overrides" /> replacing or adding what a user would type.</summary>
    public FormUrlEncodedContent ToContent(params IEnumerable<KeyValuePair<string, string>> overrides)
    {
        var list = new List<KeyValuePair<string, string>>(Fields);

        foreach (var (name, value) in overrides)
        {
            list.RemoveAll(f => f.Key == name);
            list.Add(new KeyValuePair<string, string>(name, value));
        }

        return new FormUrlEncodedContent(list);
    }

    private static Dictionary<string, string> Attributes(string tag)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in AttributeRegex().Matches(tag))
        {
            var value = match.Groups["dq"].Success ? match.Groups["dq"].Value : match.Groups["sq"].Success ? match.Groups["sq"].Value : match.Groups["bare"].Value;
            result[match.Groups["name"].Value] = WebUtility.HtmlDecode(value);
        }

        return result;
    }

    [GeneratedRegex(@"(?<open><form\b[^>]*>)(?<body>.*?)</form\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex FormRegex();

    [GeneratedRegex(@"<input\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex InputRegex();

    // The opening tag itself is the first "attribute" this matches (form, input); it is harmless in the dictionary.
    [GeneratedRegex(@"(?<name>[\w:-]+)(?:\s*=\s*(?:""(?<dq>[^""]*)""|'(?<sq>[^']*)'|(?<bare>[^\s""'>]+)))?", RegexOptions.IgnoreCase)]
    private static partial Regex AttributeRegex();
}
