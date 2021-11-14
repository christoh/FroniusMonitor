using De.Hochstaetter.Fronius.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models
{
    public static partial class Extensions
    {
        public static string ToDeviceString(this int deviceType)
        {
            return deviceType switch
            {
                -1 => Resources.NA,
                1 => "Fronius Gen24",
                67 => "Fronius Primo 15.0-1 208-240",
                68 => "Fronius Primo 12.5-1 208-240",
                69 => "Fronius Primo 11.4-1 208-240",
                70 => "Fronius Primo 10.0-1 208-240",
                71 => "Fronius Symo 15.0-3 208",
                72 => "Fronius Eco 27.0-3-S",
                73 => "Fronius Eco 25.0-3-S",
                75 => "Fronius Primo 6.0-1",
                76 => "Fronius Primo 5.0-1",
                77 => "Fronius Primo 4.6-1",
                78 => "Fronius Primo 4.0-1",
                79 => "Fronius Primo 3.6-1",
                80 => "Fronius Primo 3.5-1",
                81 => "Fronius Primo 3.0-1",
                82 => "Fronius Symo Hybrid 4.0-3-S",
                83 => "Fronius Symo Hybrid 3.0-3-S",
                84 => "Fronius IG Plus 120 V-1",
                85 => "Fronius Primo 3.8-1 208-240",
                86 => "Fronius Primo 5.0-1 208-240",
                87 => "Fronius Primo 6.0-1 208-240",
                88 => "Fronius Primo 7.6-1 208-240",
                89 => "Fronius Symo 24.0-3 USA Dummy",
                90 => "Fronius Symo 24.0-3 480",
                91 => "Fronius Symo 22.7-3 480",
                92 => "Fronius Symo 20.0-3 480",
                93 => "Fronius Symo 17.5-3 480",
                94 => "Fronius Symo 15.0-3 480",
                95 => "Fronius Symo 12.5-3 480",
                96 => "Fronius Symo 10.0-3 480",
                97 => "Fronius Symo 12.0-3 208-240",
                98 => "Fronius Symo 10.0-3 208-240",
                99 => "Fronius Symo Hybrid 5.0-3-S",
                100 => "Fronius Primo 8.2-1 Dummy",
                101 => "Fronius Primo 8.2-1 208-240",
                102 => "Fronius Primo 8.2-1",
                103 => "Fronius Agilo TL 360.0-3",
                104 => "Fronius Agilo TL 460.0-3",
                105 => "Fronius Symo 7.0-3-M",
                106 => "Fronius Galvo 3.1-1 208-240",
                107 => "Fronius Galvo 2.5-1 208-240",
                108 => "Fronius Galvo 2.0-1 208-240",
                109 => "Fronius Galvo 1.5-1 208-240",
                110 => "Fronius Symo 6.0-3-M",
                111 => "Fronius Symo 4.5-3-M",
                112 => "Fronius Symo 3.7-3-M",
                113 => "Fronius Symo 3.0-3-M",
                114 => "Fronius Symo 17.5-3-M",
                115 => "Fronius Symo 15.0-3-M",
                116 => "Fronius Agilo 75.0-3 Outdoor",
                117 => "Fronius Agilo 100.0-3 Outdoor",
                118 => "Fronius IG Plus 55 V-1",
                119 => "Fronius IG Plus 55 V-2",
                120 => "Fronius Symo 20.0-3 Dummy",
                121 => "Fronius Symo 20.0-3-M",
                122 => "Fronius Symo 5.0-3-M",
                123 => "Fronius Symo 8.2-3-M",
                124 => "Fronius Symo 6.7-3-M",
                125 => "Fronius Symo 5.5-3-M",
                126 => "Fronius Symo 4.5-3-S",
                127 => "Fronius Symo 3.7-3-S",
                128 => "Fronius IG Plus 60 V-2",
                129 => "Fronius IG Plus 60 V-1",
                130 => "SPR 8001F-3 EU",
                131 => "Fronius IG Plus 25 V-1",
                132 => "Fronius IG Plus 100 V-3",
                133 => "Fronius Agilo 100.0-3",
                134 => "SPR 3001F-1 EU",
                135 => "Fronius IG Plus V/A 10.0-3 Delta",
                _ => Resources.Unknown
            };
        }
    }
}
