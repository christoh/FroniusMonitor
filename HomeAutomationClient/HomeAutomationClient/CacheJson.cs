using System.Text.Json;
using System.Text.Json.Serialization;

namespace De.Hochstaetter.HomeAutomationClient;

/// <summary>
/// How every <see cref="ICache"/> reads and writes its values.
/// </summary>
/// <remarks>
/// <para>
/// One set of options for all heads. The file based caches and the browser's local storage hold the same data
/// under the same keys, so they have to agree on what it looks like - and the browser once disagreed in a way
/// that mattered: it serialized with Newtonsoft, which honors neither
/// <see cref="JsonIgnoreAttribute"/> nor <c>XmlIgnore</c>, and so wrote
/// <see cref="WebConnection.Password"/> into local storage in clear text, beside the encrypted one.
/// </para>
/// <para>
/// The same object is used for reading and for writing. Half of what is set here only takes effect on the way
/// in, and an option granted to one side alone is a trap: writing <see cref="double.NaN"/> is allowed by
/// <see cref="JsonNumberHandling.AllowNamedFloatingPointLiterals"/>, and a reader without that permission throws
/// on the value its own writer produced.
/// </para>
/// <para>
/// Serialization is reflection based, which needs the types of the cached values to survive the trimmer. Every
/// head builds with a trim mode that leaves our own assemblies alone; hand these options a
/// <see cref="JsonSerializerContext"/> before that changes.
/// </para>
/// </remarks>
public static class CacheJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

        // Keeps ObservableValidator.HasErrors out, which every cached BindableBase carries and none of them wants
        // written. Blunter than that goal: it drops *every* get-only property, so a cached type that keeps state in
        // one - a get-only collection, a computed id - would lose it without a word. Give such a property a setter,
        // or take this off and remove HasErrors with a type info modifier instead.
        IgnoreReadOnlyProperties = true,

        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals | JsonNumberHandling.AllowReadingFromString,
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true,
    };
}
