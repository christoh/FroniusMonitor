using Avalonia.Media.Immutable;

namespace De.Hochstaetter.HomeAutomationClient.Models.Gen24;

public static class StatusColor
{
    public static bool IsDark => Application.Current?.ActualThemeVariant.Key is "Dark";
    public static readonly IImmutableSolidColorBrush RunningOuter = new ImmutableSolidColorBrush(Color.FromUInt32(0xff003060));
    public static readonly IImmutableSolidColorBrush RunningInner = new ImmutableSolidColorBrush(Color.FromUInt32(0xff002030));
    public static readonly IImmutableSolidColorBrush StoppedOuter = new ImmutableSolidColorBrush(Color.FromUInt32(0xff606060));
    public static readonly IImmutableSolidColorBrush StoppedInner = new ImmutableSolidColorBrush(Color.FromUInt32(0xff202020));

    public static IImmutableSolidColorBrush ToBrush(this Gen24Status status)
    {
        return status.StatusCode switch
        {
            "STATE_ERROR" when IsDark => Brushes.DarkRed,
            "STATE_ERROR" => Brushes.Red,
            "STATE_RUNNING" when IsDark => RunningOuter,
            "STATE_RUNNING" => Brushes.AntiqueWhite,
            "STATE_WARNING" when IsDark=> Brushes.DarkOrange,
            "STATE_WARNING" => Brushes.Orange,
            "STATE_STARTUP" when IsDark => Brushes.SaddleBrown,
            "STATE_STARTUP" => Brushes.BurlyWood,
            _ when IsDark=> StoppedOuter,
            _ => Brushes.LightGray,
        };
    }

    public static IImmutableSolidColorBrush ToPanelBrush(this Gen24Status status)
    {
        return status.StatusCode switch
        {
            "STATE_ERROR" when IsDark => Brushes.DarkRed,
            "STATE_ERROR" => Brushes.OrangeRed,
            "STATE_RUNNING" when IsDark => RunningInner,
            "STATE_RUNNING" => Brushes.Cornsilk,
            "STATE_WARNING" when IsDark=> Brushes.DarkOrange,
            "STATE_WARNING" => Brushes.Orange,
            _ when IsDark=> StoppedInner,
            _ => Brushes.Gainsboro,
        };
    }
}