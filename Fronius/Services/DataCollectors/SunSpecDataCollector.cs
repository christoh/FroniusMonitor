namespace De.Hochstaetter.Fronius.Services.DataCollectors
{
    public class SunSpecDataCollector : IHomeAutomationRunner
    {
        public Task StartAsync(CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task StopAsync(CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TODO release managed resources here
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
