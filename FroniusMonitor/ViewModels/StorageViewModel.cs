using System.Globalization;
using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Localization;
using De.Hochstaetter.Fronius.Models;
using FroniusMonitor;

namespace De.Hochstaetter.FroniusMonitor.ViewModels
{
    public class StorageViewModel : BindableBase
    {
        private Timer timer = null!;
        private int updateSemaphore;
        private readonly IWebClientService webService;

        public StorageViewModel(IWebClientService webService)
        {
            this.webService = webService;
            webService.InverterConnection = new InverterConnection {BaseUrl = App.Settings.BaseUrl!};
        }

        private Storage storage = null!;

        public Storage Storage
        {
            get => storage;
            set => Set(ref storage, value, () => NotifyOfPropertyChange(nameof(PowerString)));
        }

        private bool isConnected;

        public bool IsConnected
        {
            get => isConnected;
            set => Set(ref isConnected, value);
        }

        public string PowerString => string.Format(CultureInfo.CurrentCulture, Resources.StoragePowerMessage, Storage.Power < 0 ? Resources.Discharging : Resources.Charging, Math.Abs(Storage.Power));

        public Task OnInitialize()
        {
            timer = new Timer(TimerElapsed, null, 0, 1000);
            return Task.CompletedTask;
        }

        public async void TimerElapsed(object? _)
        {
            if (Interlocked.Exchange(ref updateSemaphore, 1) != 0)
            {
                return;
            }

            try
            {
                var result = await webService.GetStorageDevices().ConfigureAwait(false);
                var device=result.Storages.FirstOrDefault(s => s.Id == Storage.Id);

                IsConnected = true;

                if (device != null && device.StorageTimestamp != Storage.StorageTimestamp)
                {
                    Storage = device;
                }
            }
            catch
            {
                IsConnected = false;
            }
            finally
            {
                Interlocked.Exchange(ref updateSemaphore, 0);
            }
        }
    }
}
