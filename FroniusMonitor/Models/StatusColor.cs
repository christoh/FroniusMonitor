using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Gen24;

namespace De.Hochstaetter.FroniusMonitor.Models;

public static class StatusColor
{
    public static Color ToColor(this Gen24Status status)
    {
        return status.StatusCode switch
        {
            "STATE_ERROR" => Colors.Red,
            "STATE_RUNNING" => Colors.AntiqueWhite,
            "STATE_WARNING" => Colors.Yellow,
            _ => Colors.LightGray,
        };
    }

    public static SolidColorBrush ToBrush(this Gen24Status status)
    {
        return status.StatusCode switch
        {
            "STATE_ERROR" => Brushes.Red,
            "STATE_RUNNING" => Brushes.AntiqueWhite,
            "STATE_WARNING" => Brushes.Yellow,
            _ => Brushes.LightGray,
        };
    }
}