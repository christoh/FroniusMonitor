using System.Globalization;
using System.Text.Json.Nodes;
using De.Hochstaetter.Fronius.Extensions;
using Newtonsoft.Json.Linq;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// That reading a scalar out of inverter JSON answers what it used to.
/// </summary>
/// <remarks>
/// <para>
/// <c>Gen24JsonService</c> reads everything as text and converts from there, because the inverter is not
/// consistent about whether it writes a number as a number or as a string. That worked on
/// <c>JToken.Value&lt;string&gt;()</c> of Newtonsoft, which coerced whatever it was given;
/// <c>JsonNode.GetValue&lt;string&gt;()</c> of System.Text.Json throws instead, so
/// <see cref="JsonExtensions.AsString"/> had to be written by hand - and a hand written coercion that is subtly
/// different from the old one is exactly the kind of change that shows up months later as a channel reading zero.
/// </para>
/// <para>
/// So the old implementation is the oracle here: Newtonsoft is still referenced for the WattPilot protocol, which
/// makes it possible to feed the same JSON to both and compare. These tests are the reason to believe the
/// conversion did not change what the models end up holding.
/// </para>
/// </remarks>
public class JsonExtensionsTests
{
    private const string Scalars =
        """
        {
          "text": "a string",
          "numberAsText": "5",
          "decimalAsText": "1.5",
          "integer": 5,
          "negative": -1,
          "decimal": 1.5,
          "trailingZero": 1.50,
          "exponent": 1e3,
          "big": 9007199254740993,
          "bigNegative": -9007199254740993,
          "zero": 0,
          "yes": true,
          "no": false,
          "nothing": null,
          "object": { "a": 1 },
          "array": [ 1, 2 ]
        }
        """;

    private static JsonNode Node => JsonNode.Parse(Scalars)!;

    private static JToken Token => JToken.Parse(Scalars);

    [Theory]
    [InlineData("text")]
    [InlineData("numberAsText")]
    [InlineData("decimalAsText")]
    [InlineData("integer")]
    [InlineData("negative")]
    [InlineData("decimal")]
    [InlineData("trailingZero")]
    [InlineData("exponent")]
    [InlineData("big")]
    [InlineData("bigNegative")]
    [InlineData("zero")]
    [InlineData("yes")]
    [InlineData("no")]
    [InlineData("nothing")]
    [InlineData("missing")]
    public void A_scalar_reads_as_the_same_text_Newtonsoft_gave(string name)
    {
        Assert.Equal(Token[name]?.Value<string>(), Node[name].AsString());
    }

    [Fact]
    public void A_whole_number_keeps_every_digit_it_was_written_with()
    {
        // A serial number is a whole number long enough to lose its last digit to a double, so a whole number is
        // handed on as the inverter wrote it. Newtonsoft did the same, by holding whole numbers in a long.
        Assert.Equal("9007199254740993", Node["big"].AsString());
        Assert.Equal("-9007199254740993", Node["bigNegative"].AsString());
    }

    [Fact]
    public void A_number_written_with_an_exponent_still_reads_into_a_whole_number()
    {
        // 1e3 is valid JSON, and Convert.ChangeType will not read that text into an int - so anything that is not
        // plain digits is written out in full first. Otherwise an inverter writing a channel that way would turn a
        // reading of 1000 into a parse failure.
        Assert.Equal("1000", Node["exponent"].AsString());
        Assert.Equal(1000, Node["exponent"].AsInt32());
    }

    [Theory]
    [InlineData("object")]
    [InlineData("array")]
    public void Something_that_is_not_a_scalar_has_no_text(string name)
    {
        // Newtonsoft threw an InvalidCastException here. Nothing in the reader relies on that: it asks for the
        // text of a leaf, and null is what "there is nothing to read" means everywhere else in it.
        Assert.Null(Node[name].AsString());
    }

    [Theory]
    [InlineData("integer", 5d)]
    [InlineData("negative", -1d)]
    [InlineData("decimal", 1.5)]
    [InlineData("decimalAsText", 1.5)]
    [InlineData("exponent", 1000d)]
    [InlineData("zero", 0d)]
    public void A_number_reads_as_a_number_whether_it_was_written_as_one_or_as_text(string name, double expected)
    {
        Assert.Equal(expected, Node[name].AsDouble());
    }

    [Theory]
    [InlineData("text")]
    [InlineData("nothing")]
    [InlineData("missing")]
    [InlineData("object")]
    public void What_is_not_a_number_reads_as_none(string name)
    {
        Assert.Null(Node[name].AsDouble());
        Assert.Null(Node[name].AsInt32());
    }

    [Theory]
    [InlineData("yes", true)]
    [InlineData("no", false)]
    [InlineData("integer", true)]
    [InlineData("zero", false)]
    public void A_flag_reads_from_true_and_false_and_from_one_and_zero(string name, bool expected)
    {
        // An inverter writes a channel flag as 1 or 0 and a config flag as true or false.
        Assert.Equal(expected, Node[name].AsBoolean());
    }

    [Fact]
    public void A_decimal_separator_is_read_the_same_whatever_the_current_culture_is()
    {
        // The JSON always uses a point. Reading it under a culture that writes a comma must not turn 1.5 into 15.
        var german = CultureInfo.GetCultureInfo("de-DE");
        var previous = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = german;
            Assert.Equal(1.5, Node["decimal"].AsDouble());
            Assert.Equal(1.5, Node["decimalAsText"].AsDouble());
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData("{}", false)]
    [InlineData("[]", false)]
    [InlineData("""{ "a": 1 }""", true)]
    [InlineData("[1]", true)]
    [InlineData("5", true)]
    public void HasValues_says_whether_there_is_anything_in_it(string json, bool expected)
    {
        Assert.Equal(expected, JsonNode.Parse(json).HasValues());
    }

    [Theory]
    [InlineData("{}", false)]
    [InlineData("""{ "a": {} }""", false)]
    [InlineData("""{ "a": { "b": { "c": {} } } }""", false)]
    [InlineData("""{ "a": { "b": { "c": 1 } } }""", true)]
    [InlineData("""{ "a": [ {} ] }""", false)]
    [InlineData("""{ "a": [ 1 ] }""", true)]
    public void HasAnyValue_looks_all_the_way_down(string json, bool expected)
    {
        // A delta built one nested object at a time is a tree of empty objects when nothing changed, and that is
        // not something to send to an inverter. HasValues cannot tell those apart.
        Assert.Equal(expected, JsonNode.Parse(json).HasAnyValue());
    }
}
