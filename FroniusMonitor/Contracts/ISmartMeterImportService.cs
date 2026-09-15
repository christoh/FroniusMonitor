using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.FroniusMonitor.Contracts;

public interface ISmartMeterImportService
{
    public Task ImportSmartMeterData(object parameters);
}