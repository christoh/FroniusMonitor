using System.Reflection;

namespace De.Hochstaetter.HomeAutomationClient.Misc;

internal static class GaugeColors
{
    public static ImmutableArray<ColorThreshold> HighIsBad { get; } =
    [
        new(0, Colors.Green),
        new(.75, Colors.YellowGreen),
        new(.95, Colors.OrangeRed),
        new(1, Colors.Red),
    ];

    public static ImmutableArray<ColorThreshold> MidIsGood { get; } =
    [
        new(0, Colors.Red),
        new(.05, Colors.OrangeRed),
        new(.25, Colors.YellowGreen),
        new(0.5, Colors.Green),
        new(.75, Colors.YellowGreen),
        new(.95, Colors.OrangeRed),
        new(1, Colors.Red),
    ];

    public static ImmutableArray<ColorThreshold> ExtremeIsBad { get; } =
    [
        new(0, Colors.Red),
        new(.01, Colors.OrangeRed),
        new(.10, Colors.YellowGreen),
        new(0.20, Colors.Green),
        new(0.70, Colors.Green),
        new(.90, Colors.YellowGreen),
        new(.99, Colors.OrangeRed),
        new(1, Colors.Red),
    ];

    public static ImmutableArray<ColorThreshold> OneThirdIsGood { get; } =
    [
        new(0, Colors.Red),
        new(.2, Colors.OrangeRed),
        new(.3, Colors.YellowGreen),
        new(1d / 3d, Colors.Green),
        new(.36333333, Colors.YellowGreen),
        new(.5, Colors.OrangeRed),
        new(1, Colors.Red),
    ];

    public static ImmutableArray<ColorThreshold> MidIsBad { get; } =
    [
        new(0, Colors.Green),
        new(.20, Colors.YellowGreen),
        new(.3333333, Colors.OrangeRed),
        new(0.5, Colors.Red),
        new(.6666667, Colors.OrangeRed),
        new(.8, Colors.YellowGreen),
        new(1, Colors.Green),
    ];

    public static ImmutableArray<ColorThreshold> HigherThan15IsBad { get; } =
    [
        new(0, Colors.Green),
        new(.10, Colors.YellowGreen),
        new(.15, Colors.OrangeRed),
        new(1, Colors.Red),
    ];

    public static ImmutableArray<ColorThreshold> LowIsBad { get; } =
    [
        new(0, Colors.Red),
        new(.05, Colors.OrangeRed),
        new(.5, Colors.YellowGreen),
        new(1, Colors.Green),
    ];

    public static ImmutableArray<ColorThreshold> VeryHighIsGood { get; } =
    [
        new(0, Colors.Red),
        new(.7, Colors.OrangeRed),
        new(.9, Colors.YellowGreen),
        new(1, Colors.Green),
    ];

    public static ImmutableArray<ColorThreshold> VeryLowIsBad { get; } =
    [
        new(0, Colors.Red),
        new(.025, Colors.OrangeRed),
        new(.1, Colors.YellowGreen),
        new(1, Colors.Green),
    ];

    public static ImmutableArray<ColorThreshold> AllIsGood { get; } =
    [
        new(0, Colors.Green),
        new(1, Colors.Green),
    ];

    public static IEnumerable<ColorThreshold> Parse(string name) =>
        typeof(GaugeColors).GetFields(BindingFlags.Public | BindingFlags.Static)
            .First(f => f.Name == name).GetValue(null)
            as IEnumerable<ColorThreshold> 
        ?? throw new NullReferenceException("GaugeColor not found");
}
