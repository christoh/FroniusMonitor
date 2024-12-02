using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Contracts;

public interface ISmartMeterImportService
{
    public Task ImportSmartMeterData(object parameters);
}