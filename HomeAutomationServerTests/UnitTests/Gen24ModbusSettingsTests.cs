using De.Hochstaetter.Fronius;
using De.Hochstaetter.Fronius.Localization;
using De.Hochstaetter.Fronius.Models.Gen24.Settings;
using De.Hochstaetter.Fronius.Services;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// What the Modbus settings do with a value they will not take, and what a copy of them shares with the original.
/// </summary>
/// <remarks>
/// Both were bugs the user saw as "Undo does not work". A setter that refuses to store a value leaves the text box
/// holding it, because a binding only pushes to its control when the value it reads has changed - so Undo had
/// nothing to put back. And a shallow copy of an <c>ObservableValidator</c> hands the copy the error store of the
/// original, so the copy would arrive carrying errors that belong to the object it replaces.
/// </remarks>
public class Gen24ModbusSettingsTests
{
    public Gen24ModbusSettingsTests()
    {
        // GetToken reaches for IGen24JsonService through the static IoC, the way the models do throughout.
        IoC.Update(new ServiceCollection().AddSingleton<IGen24JsonService, Gen24JsonService>().BuildServiceProvider());
    }

    private static Gen24ModbusSettings AllFieldsSet() => new()
    {
        Rtu0 = ModbusInterfaceRole.Slave,
        Rtu1 = ModbusInterfaceRole.Master,
        BaudRate = 19200,
        IsDemoMode = false,
        MeterAddress = 200,
        Mode = ModbusSlaveMode.Both,
        Parity = ModbusParity.Even,
        TcpPort = 1502,
        SunSpecAddress = 240,
        InverterAddress = 1,
        SunspecMode = Fronius.Models.Gen24.Settings.SunspecMode.Int,
        AllowControl = true,
        RestrictControl = true,
        IpAddress = "192.168.178.1,10.0.0.0/8",
    };

    [Fact]
    public void A_refused_meter_address_is_stored_and_reported()
    {
        var settings = AllFieldsSet();

        settings.MeterAddress = 0;

        // Stored on purpose: a value the model throws away is a value the view keeps on screen with nothing to
        // replace it, which is exactly what broke Undo.
        Assert.Equal<byte?>(0, settings.MeterAddress);
        Assert.True(settings.HasErrors);
        Assert.Equal(Resources.MeterAddressError, settings.GetErrors(nameof(Gen24ModbusSettings.MeterAddress)).Single().ErrorMessage);
    }

    [Fact]
    public void A_sunspec_address_of_zero_is_refused()
    {
        var settings = AllFieldsSet();

        settings.SunSpecAddress = 0;

        Assert.True(settings.HasErrors);
        Assert.Equal(Resources.SunspecAddressError, settings.GetErrors(nameof(Gen24ModbusSettings.SunSpecAddress)).Single().ErrorMessage);
    }

    [Fact]
    public void An_ip_address_that_is_not_a_list_of_addresses_with_optional_masks_is_refused()
    {
        var settings = AllFieldsSet();

        settings.IpAddress = "192.168.178.1,nonsense";

        Assert.True(settings.HasErrors);
        Assert.Equal(Resources.MustBeIpv4Address, settings.GetErrors(nameof(Gen24ModbusSettings.IpAddress)).Single().ErrorMessage);
    }

    [Fact]
    public void Putting_an_acceptable_value_back_clears_the_error()
    {
        var settings = AllFieldsSet();
        settings.MeterAddress = 0;

        settings.MeterAddress = 200;

        Assert.False(settings.HasErrors);
        Assert.Empty(settings.GetErrors(nameof(Gen24ModbusSettings.MeterAddress)));
    }

    [Fact]
    public void An_empty_field_is_not_a_refused_field()
    {
        var settings = AllFieldsSet();

        settings.MeterAddress = null;
        settings.SunSpecAddress = null;
        settings.IpAddress = null;

        Assert.False(settings.HasErrors);
    }

    [Fact]
    public void An_address_that_was_left_empty_is_left_out_of_the_delta_rather_than_cleared()
    {
        // The string control address is a Tauro thing, so it may well be empty. Leaving it out means the inverter
        // keeps whatever it has: an empty box says "not mine to say", never "clear it".
        var loaded = AllFieldsSet();
        var edited = (Gen24ModbusSettings)loaded.Clone();

        edited.SunSpecAddress = null;

        Assert.False(edited.HasErrors);
        Assert.False(edited.GetToken(loaded).HasValues);
    }

    [Fact]
    public void A_clone_holds_the_same_values_as_the_original()
    {
        var original = AllFieldsSet();

        var clone = (Gen24ModbusSettings)original.Clone();

        // The delta the server would write is the strictest check there is: it walks every field that goes to the
        // inverter, so a field the hand written Clone forgot shows up here.
        Assert.False(clone.GetToken(original).HasValues);
        Assert.Equal(original.Rtu0, clone.Rtu0);
        Assert.Equal(original.Rtu1, clone.Rtu1);
        Assert.Equal(original.IpAddress, clone.IpAddress);
        Assert.Equal(original.MeterAddress, clone.MeterAddress);
        Assert.Equal(original.SunSpecAddress, clone.SunSpecAddress);
    }

    [Fact]
    public void A_clone_has_a_validation_state_of_its_own()
    {
        // This is the Undo of the dialog: what it read from the inverter is kept aside and copied for editing.
        var loaded = AllFieldsSet();
        var edited = (Gen24ModbusSettings)loaded.Clone();

        edited.MeterAddress = 0;

        Assert.True(edited.HasErrors);

        // A shallow copy shares the error store of an ObservableValidator while keeping an error count of its own,
        // so the two would report validation states that contradict each other: GetErrors is what sees the store,
        // HasErrors only the count. Undo would then bring back an object still carrying the errors it replaced.
        Assert.Empty(loaded.GetErrors());
        Assert.False(loaded.HasErrors);
        Assert.Empty(((Gen24ModbusSettings)loaded.Clone()).GetErrors());
    }

    [Fact]
    public void A_clone_of_a_refused_value_is_refused_as_well()
    {
        // Copying is faithful, errors included: the copy validates what it is given rather than trusting it.
        var original = AllFieldsSet();
        original.MeterAddress = 0;

        var clone = (Gen24ModbusSettings)original.Clone();

        Assert.Equal<byte?>(0, clone.MeterAddress);
        Assert.Equal(Resources.MeterAddressError, clone.GetErrors(nameof(Gen24ModbusSettings.MeterAddress)).Single().ErrorMessage);
    }

    [Fact]
    public void A_clone_does_not_inherit_the_subscribers_of_the_original()
    {
        var original = AllFieldsSet();
        var notifications = 0;
        original.PropertyChanged += (_, _) => notifications++;

        var clone = (Gen24ModbusSettings)original.Clone();
        clone.MeterAddress = 100;

        Assert.Equal(0, notifications);
    }
}
