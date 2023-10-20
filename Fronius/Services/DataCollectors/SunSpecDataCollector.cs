using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Services.DataCollectors
{
    public class SunSpecDataCollector:IDataCollector
    {
        public Task StartAsync(CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task StopAsync(CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }
    }
}
