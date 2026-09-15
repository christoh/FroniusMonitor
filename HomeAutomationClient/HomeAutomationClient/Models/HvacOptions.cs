using De.Hochstaetter.Fronius.Models.ToshibaAc;

namespace De.Hochstaetter.HomeAutomationClient.Models;

/// <summary>
/// One entry of a Toshiba control's choice menu - a fan speed, a power limit, a merit feature, a swing mode, a
/// temperature. The menu item binds <see cref="IsSelected"/> to its check mark and <see cref="SelectCommand"/> to
/// its click; <see cref="ToshibaHvacViewModel"/> keeps <see cref="IsSelected"/> in step with the device's state.
/// </summary>
public abstract partial class HvacOption : ObservableObject
{
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public abstract object ValueObject { get; }

    protected abstract Task Apply();

    [RelayCommand]
    private Task Select() => Apply();
}

public abstract class HvacOption<T>(T value, Func<T, Task> apply) : HvacOption where T : struct
{
    public T Value => value;

    public override object ValueObject => value;

    protected override Task Apply() => apply(value);

    public override string ToString() => value.ToString() ?? string.Empty;
}

// One concrete class per kind, because a XAML DataTemplate names its type and cannot name a generic instantiation.

public sealed class FanSpeedOption(ToshibaHvacFanSpeed value, Func<ToshibaHvacFanSpeed, Task> apply) : HvacOption<ToshibaHvacFanSpeed>(value, apply);

public sealed class PowerLimitOption(byte value, Func<byte, Task> apply) : HvacOption<byte>(value, apply)
{
    public string Text => $"{Value} %";
}

public sealed class MeritFeatureOption(ToshibaHvacMeritFeaturesA value, Func<ToshibaHvacMeritFeaturesA, Task> apply) : HvacOption<ToshibaHvacMeritFeaturesA>(value, apply);

public sealed class SwingModeOption(ToshibaHvacSwingMode value, Func<ToshibaHvacSwingMode, Task> apply) : HvacOption<ToshibaHvacSwingMode>(value, apply);

/// <summary>
/// <see cref="HvacOption{T}.Value"/> is the byte the device takes; <see cref="DisplayOffset"/> is what the device
/// adds to the temperature it means - 16 in the 8 °C heating mode, 0 otherwise - so <see cref="Text"/> shows the
/// temperature as the set temperature display does. The view model rewrites the offset in place, like
/// <see cref="HvacOption.IsSelected"/>; the list itself is never rebuilt for a mode change, because it is bound to a
/// menu and the state changes arrive on the hub's thread.
/// </summary>
public sealed partial class TemperatureOption(sbyte value, Func<sbyte, Task> apply) : HvacOption<sbyte>(value, apply)
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Text))]
    public partial sbyte DisplayOffset { get; set; }

    public string Text => $"{Value - DisplayOffset:00} °C";
}
