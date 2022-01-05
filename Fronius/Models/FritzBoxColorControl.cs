using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace De.Hochstaetter.Fronius.Models;

[Flags]
public enum FritzBoxColorMode
{
    Hsv = 1,
    Temperature = 4,
}

[XmlType("colorcontrol")]
public class FritzBoxColorControl:BindableBase
{
    private FritzBoxColorMode supportedModes;

    public FritzBoxColorMode SupportedModes
    {
        get => supportedModes;
        set => Set(ref supportedModes, value);
    }
}