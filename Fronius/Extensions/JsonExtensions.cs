using System.Text.Json;
using System.Text.Json.Nodes;

namespace De.Hochstaetter.Fronius.Extensions;

/// <summary>
/// The handful of things <c>System.Text.Json</c> does not offer that reading a Fronius inverter needs.
/// </summary>
/// <remarks>
/// The inverter is not consistent about the type it writes a value as - a number can arrive as <c>5</c> or as
/// <c>"5"</c> from one firmware to the next - and <c>Gen24JsonService</c> deals with that by reading everything as
/// text and converting from there. <c>JToken.Value&lt;string&gt;()</c> of Newtonsoft did that conversion for it;
/// <see cref="JsonNode.GetValue{T}"/> does not, it throws when the value is not already the type asked for. Hence
/// <see cref="AsString"/>, and the two typed readers beside it for the places that want a number or a flag
/// directly.
/// </remarks>
public static class JsonExtensions
{
    /// <summary>
    /// A scalar as text, whatever it was written as, or <see langword="null"/> where there is nothing there.
    /// The replacement for <c>Value&lt;string&gt;()</c>, and it answers the same for every kind of scalar.
    /// </summary>
    /// <remarks>
    /// A whole number comes back as it was written, so a long serial number keeps its last digit. An object or an
    /// array has no text of its own and answers null, where Newtonsoft threw.
    /// </remarks>
    public static string? AsString(this JsonNode? node)
    {
        if (node is not JsonValue value)
        {
            return null;
        }

        if (value.TryGetValue<string>(out var text))
        {
            return text;
        }

        if (value.TryGetValue<bool>(out var flag))
        {
            // "True" and "False", as Convert.ToString and Newtonsoft both write them.
            return flag ? bool.TrueString : bool.FalseString;
        }

        // A parsed node carries its number as it was written; one built in code does not, so both are tried.
        if (value.TryGetValue<JsonElement>(out var element))
        {
            return element.ValueKind is JsonValueKind.Number ? NumberAsString(element.GetRawText()) : element.ToString();
        }

        return value.TryGetValue<double>(out var number)
            ? number.ToString(CultureInfo.InvariantCulture)
            : value.ToJsonString().Trim('"');
    }

    /// <summary>
    /// A JSON number as the text the rest of the reader expects to convert from.
    /// </summary>
    /// <remarks>
    /// Plain digits are handed on untouched, which keeps a serial number or an identifier exact however long it
    /// is. Anything else - a decimal, or an exponent - goes through a <see cref="double"/> first, because
    /// <c>1e3</c> is valid JSON that <see cref="Convert.ChangeType(object,Type,IFormatProvider)"/> will not read
    /// into an <see cref="int"/>, and an inverter writing a channel that way would otherwise fail to parse where
    /// it used to give 1000. That is what Newtonsoft did: whole numbers through a <see cref="long"/>, the rest
    /// through a <see cref="double"/>.
    /// </remarks>
    private static string NumberAsString(string rawText)
    {
        var digits = rawText.AsSpan(rawText.StartsWith('-') ? 1 : 0);

        return digits.IndexOfAnyExcept("0123456789") < 0
            ? rawText
            : Convert.ToString(double.Parse(rawText, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture) ?? rawText;
    }

    /// <summary>A scalar as a number, or <see langword="null"/> where there is nothing there or it is not one.</summary>
    public static double? AsDouble(this JsonNode? node) =>
        double.TryParse(node.AsString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value) ? value : null;

    /// <summary>A scalar as a whole number, or <see langword="null"/> where there is nothing there or it is not one.</summary>
    public static int? AsInt32(this JsonNode? node) =>
        int.TryParse(node.AsString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    /// <inheritdoc cref="AsInt32"/>
    public static uint? AsUInt32(this JsonNode? node) =>
        uint.TryParse(node.AsString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    /// <summary>A scalar as a flag, taking the <c>1</c> and <c>0</c> an inverter writes as well as true and false.</summary>
    public static bool? AsBoolean(this JsonNode? node) => node.AsString() switch
    {
        null => null,
        var text when bool.TryParse(text, out var flag) => flag,
        var text when double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) => number != 0,
        _ => null,
    };

    /// <summary>
    /// Whether there is anything in here at all - the replacement for <c>HasValues</c>, which the delta of a
    /// settings write is judged by: an empty object means the inverter already holds what was asked for.
    /// </summary>
    /// <remarks>
    /// Not quite what Newtonsoft answered: <c>HasValues</c> meant "has children", so a scalar was false. Here a
    /// scalar is true, because the question being asked is whether there is something to send.
    /// </remarks>
    public static bool HasValues(this JsonNode? node) => node switch
    {
        JsonObject jsonObject => jsonObject.Count > 0,
        JsonArray jsonArray => jsonArray.Count > 0,
        null => false,
        _ => true,
    };

    /// <summary>
    /// Whether anywhere in here, however deep, there is an actual value rather than just more empty objects.
    /// </summary>
    /// <remarks>
    /// A delta built one nested object at a time ends up as a tree of empty objects when nothing changed, and
    /// that is not something to send to an inverter. <see cref="HasValues"/> cannot tell the two apart, because
    /// an object holding one empty object does have a value in it by that reckoning.
    /// </remarks>
    public static bool HasAnyValue(this JsonNode? node) => node switch
    {
        JsonValue => true,
        JsonObject jsonObject => jsonObject.Any(property => property.Value.HasAnyValue()),
        JsonArray jsonArray => jsonArray.Any(entry => entry.HasAnyValue()),
        _ => false,
    };
}
