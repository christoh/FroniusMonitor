using De.Hochstaetter.Fronius.Localization;
using De.Hochstaetter.Fronius.Validators;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The reusable validation rules of <c>Fronius/Validators</c>. They are the one place that decides whether a value
/// a user typed is acceptable, for the WPF app, the Avalonia client and the request bodies of this server alike, so
/// they are worth testing on their own rather than through a dialog.
/// </summary>
public class ValidationRuleTests
{
    /// <summary>
    /// Runs a rule the way a framework does. A <see cref="ValidationAttribute"/> reports success as
    /// <see langword="null"/>, so a message coming back is a refusal.
    /// </summary>
    private static string? Validate(ValidationAttribute rule, object? value) => rule.GetValidationResult(value, new ValidationContext(new object()))?.ErrorMessage;

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(123)]
    [InlineData(246)]
    [InlineData(247)]
    public void MinMaxInt_takes_a_value_in_range_including_its_bounds(int value)
    {
        Assert.Null(Validate(new MinMaxIntAttribute(1, 247), value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(248)]
    [InlineData(1000)]
    public void MinMaxInt_refuses_a_value_out_of_range(int value)
    {
        Assert.NotNull(Validate(new MinMaxIntAttribute(1, 247), value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MinMaxInt_lets_an_empty_value_through(object? value)
    {
        // A field the user has not filled in is not a field with a wrong value in it. NotEmpty is what says a
        // field is needed.
        Assert.Null(Validate(new MinMaxIntAttribute(1, 247), value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_value_is_refused_where_the_field_has_to_hold_one(object? value)
    {
        // A text box bound to a string is how a number gets typed, so this is what an emptied number box hits.
        Assert.NotNull(Validate(new MinMaxIntAttribute(1, 247) { AllowEmpty = false }, value));
    }

    [Theory]
    [InlineData("not a number")]
    [InlineData("12.5")]
    [InlineData("1e3")]
    [InlineData("abc")]
    public void MinMaxInt_refuses_something_that_is_not_a_whole_number(string value)
    {
        Assert.NotNull(Validate(new MinMaxIntAttribute(1, 247), value));
    }

    [Theory]
    [InlineData("200")]
    [InlineData("0200")]
    [InlineData(" 200 ")]
    public void MinMaxInt_reads_the_value_from_its_text_so_that_a_string_bound_field_works(string value)
    {
        // Text boxes bind to strings, always, so text is the normal case and not the exception.
        Assert.Null(Validate(new MinMaxIntAttribute(1, 247), value));
    }

    [Fact]
    public void MinMaxInt_names_the_field_and_the_range_in_its_message()
    {
        var message = Validate(new MinMaxIntAttribute(0, 100) { PropertyDisplayName = "State of charge" }, 500);

        Assert.Equal(string.Format(CultureInfo.CurrentCulture, Resources.MustBeBetween, "State of charge", 0, 100), message);
    }

    [Fact]
    public void MinMaxInt_falls_back_to_a_neutral_name_where_none_was_given()
    {
        var message = Validate(new MinMaxIntAttribute(0, 100), 500);

        Assert.Equal(string.Format(CultureInfo.CurrentCulture, Resources.MustBeBetween, Resources.DefaultPropertyDisplayName, 0, 100), message);
    }

    [Fact]
    public void A_message_resource_key_replaces_the_composed_message_altogether()
    {
        // This is how a field that already has a sentence of its own keeps it - the Modbus meter address, say.
        var message = Validate(new MinMaxIntAttribute(1, 247) { MessageResourceKey = nameof(Resources.MeterAddressError) }, 0);

        Assert.Equal(Resources.MeterAddressError, message);
    }

    [Fact]
    public void A_display_name_resource_key_is_looked_up_while_the_rule_runs()
    {
        var message = Validate(new MinMaxIntAttribute(0, 100) { PropertyDisplayNameResourceKey = nameof(Resources.Modbus) }, 500);

        Assert.Equal(string.Format(CultureInfo.CurrentCulture, Resources.MustBeBetween, Resources.Modbus, 0, 100), message);
    }

    [Theory]
    [InlineData(80)]
    [InlineData(800)]
    [InlineData(123.5)]
    public void MinMaxDouble_takes_a_value_in_range(double value)
    {
        Assert.Null(Validate(new MinMaxDoubleAttribute(80, 800), value));
    }

    [Theory]
    [InlineData(79.9)]
    [InlineData(800.1)]
    public void MinMaxDouble_refuses_a_value_out_of_range(double value)
    {
        Assert.NotNull(Validate(new MinMaxDoubleAttribute(80, 800), value));
    }

    [Fact]
    public void MinMaxDouble_takes_decimals_where_MinMaxInt_would_not()
    {
        var twoAndAHalf = 2.5.ToString(CultureInfo.CurrentCulture);

        Assert.Null(Validate(new MinMaxDoubleAttribute(0, 10), twoAndAHalf));
        Assert.NotNull(Validate(new MinMaxIntAttribute(0, 10), twoAndAHalf));
    }

    [Theory]
    [InlineData("192.168.178.1")]
    [InlineData("0.0.0.0")]
    [InlineData("255.255.255.255")]
    public void Ipv4_takes_an_address(string value)
    {
        Assert.Null(Validate(new Ipv4Attribute(), value));
    }

    [Theory]
    [InlineData("192.168.178")]
    [InlineData("192.168.178.1.1")]
    [InlineData("192.168.178.256")]
    [InlineData("192.168.178.x")]
    [InlineData("::1")]
    [InlineData("nonsense")]
    public void Ipv4_refuses_something_that_is_not_an_address(string value)
    {
        Assert.NotNull(Validate(new Ipv4Attribute(), value));
    }

    [Fact]
    public void Ipv4_takes_a_mask_only_where_masks_are_switched_on()
    {
        Assert.Null(Validate(new Ipv4Attribute { AllowMask = true }, "192.168.178.0/24"));
        Assert.NotNull(Validate(new Ipv4Attribute(), "192.168.178.0/24"));
    }

    [Theory]
    [InlineData("192.168.178.0/0")]
    [InlineData("192.168.178.0/33")]
    [InlineData("192.168.178.0/x")]
    [InlineData("192.168.178.0/24/24")]
    public void Ipv4_refuses_a_mask_that_is_not_a_prefix_length(string value)
    {
        Assert.NotNull(Validate(new Ipv4Attribute { AllowMask = true }, value));
    }

    [Fact]
    public void Ipv4_takes_a_list_only_where_lists_are_switched_on()
    {
        Assert.Null(Validate(new Ipv4Attribute { AllowList = true, AllowMask = true }, "192.168.178.1,10.0.0.0/8"));
        Assert.NotNull(Validate(new Ipv4Attribute { AllowMask = true }, "192.168.178.1,10.0.0.0/8"));
    }

    [Fact]
    public void Every_entry_of_a_list_has_to_hold_on_its_own()
    {
        Assert.NotNull(Validate(new Ipv4Attribute { AllowList = true }, "192.168.178.1,nonsense"));
    }

    [Fact]
    public void Ipv4_takes_a_host_name_only_where_host_names_are_switched_on()
    {
        Assert.Null(Validate(new Ipv4Attribute { AllowHostname = true }, "home.hochstaetter.de"));
        Assert.NotNull(Validate(new Ipv4Attribute(), "home.hochstaetter.de"));
    }

    [Fact]
    public void A_host_name_with_a_mask_is_refused_because_a_mask_only_means_something_on_an_address()
    {
        Assert.NotNull(Validate(new Ipv4Attribute { AllowHostname = true, AllowMask = true }, "home.hochstaetter.de/24"));
    }

    [Fact]
    public void Ipv4_says_which_of_the_two_it_wanted()
    {
        Assert.Equal(Resources.MustBeIpv4Address, Validate(new Ipv4Attribute(), "nonsense"));
        Assert.Equal(Resources.NoHostnameOrIpv4Address, Validate(new Ipv4Attribute { AllowHostname = true }, "?"));
    }

    [Fact]
    public void An_empty_ip_address_is_left_alone()
    {
        Assert.Null(Validate(new Ipv4Attribute { AllowList = true, AllowMask = true }, null));
        Assert.Null(Validate(new Ipv4Attribute { AllowList = true, AllowMask = true }, string.Empty));
    }

    [Fact]
    public void A_regex_rule_takes_what_matches_and_refuses_what_does_not()
    {
        var rule = new RegexRuleAttribute("^100$|^0*([0-9]{1,2})$") { MessageResourceKey = nameof(Resources.MustBePercent) };

        Assert.Null(Validate(rule, "100"));
        Assert.Null(Validate(rule, "07"));
        Assert.Equal(Resources.MustBePercent, Validate(rule, "101"));
    }

    [Fact]
    public void A_regex_rule_without_a_message_says_so_rather_than_showing_the_pattern_to_a_user()
    {
        // A pattern is not something an end user can act on, so there is no composed message to fall back to and
        // leaving the message out is a programming mistake.
        var message = Validate(new RegexRuleAttribute("^a+$"), "b");

        Assert.NotNull(message);
        Assert.Contains("No message was given", message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("200", 200L)]
    [InlineData("0200", 200L)]
    [InlineData(" -12 ", -12L)]
    public void NumericText_reads_a_number_the_way_the_rule_that_passed_it_does(string text, long expected)
    {
        // The two have to agree. A view model that parsed differently from the rule would throw when it saves,
        // and only for some inputs.
        Assert.Null(Validate(new MinMaxIntAttribute(-100, 1000), text));
        Assert.Equal(expected, NumericText.ToInteger(text));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void NumericText_reads_an_empty_box_as_no_value(string? text)
    {
        Assert.Null(NumericText.ToInteger(text));
        Assert.Null(NumericText.ToNumber(text));
    }

    [Fact]
    public void NumericText_refuses_to_guess_at_text_no_rule_would_have_passed()
    {
        // Getting here means a rule and a view model disagreed, which is a defect and not a user error.
        Assert.Throws<InvalidOperationException>(() => NumericText.ToInteger("abc"));
    }

    [Fact]
    public void NumericText_puts_a_value_back_into_a_box_without_decoration()
    {
        Assert.Equal("200", NumericText.Of(200L));
        Assert.Null(NumericText.Of(null as long?));
    }
}
