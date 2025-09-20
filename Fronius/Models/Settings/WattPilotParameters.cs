using System;
using System.Collections.Generic;
using System.Text;

namespace De.Hochstaetter.Fronius.Models.Settings;

public class WattPilotParameters
{
    public IEnumerable<WebConnection>? Connections { get; set; }
}