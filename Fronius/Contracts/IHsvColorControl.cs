using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Contracts
{
    public interface IHsvColorControl
    {
        bool HasHsvColorControl { get; }
        bool IsHsvEnabled { get; }
        double? HueDegrees => HueRadians / Math.PI * 180;
        double? HueRadians => HueDegrees * Math.PI / 180;
        double? Saturation { get; }
        double? Value { get; }
        Task SetHsv(double hueDegrees, double saturation, double value);
    }
}
