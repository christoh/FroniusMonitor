namespace De.Hochstaetter.FroniusMonitor.Models;

public static class StatusColor
{
    public static Color ToColor(this Gen24Status status, bool treatStoppedAsRunning = false)
    {
        return status.StatusCode switch
        {
            "STATE_ERROR" => Colors.Red,
            "STATE_RUNNING" => Colors.AntiqueWhite,
            "STATE_WARNING" => Colors.Orange,
            "STATE_STOPPED" when treatStoppedAsRunning => Colors.AntiqueWhite,
            "STATE_STARTUP" => Colors.BurlyWood,
            _ => Colors.LightGray,
        };
    }

    public static SolidColorBrush ToBrush(this Gen24Status status, bool treatStoppedAsRunning = false)
    {
        return status.StatusCode switch
        {
            "STATE_ERROR" => Brushes.Red,
            "STATE_RUNNING" => Brushes.AntiqueWhite,
            "STATE_WARNING" => Brushes.Orange,
            "STATE_STOPPED" when treatStoppedAsRunning => Brushes.AntiqueWhite,
            "STATE_STARTUP" when treatStoppedAsRunning => Brushes.AntiqueWhite,
            "STATE_STARTUP" => Brushes.BurlyWood,
            _ => Brushes.LightGray,
        };
    }

    public static SolidColorBrush ToPanelBrush(this Gen24Status status)
    {
        return status.StatusCode switch
        {
            "STATE_ERROR" => Brushes.OrangeRed,
            "STATE_RUNNING" => Brushes.Cornsilk,
            "STATE_WARNING" => Brushes.Orange,
            "STATE_STARTUP" => Brushes.BurlyWood,
            _ => Brushes.Gainsboro,
        };
    }
}