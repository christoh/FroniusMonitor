using De.Hochstaetter.Fronius.Models.ToshibaAc;

namespace De.Hochstaetter.Fronius.Contracts
{
    public interface IToshibaAirConditionService
    {
        public ValueTask Start();
        public void Stop();
        public bool IsRunning { get; }
        public ObservableCollection<ToshibaAcMapping>? AllDevices { get; }
        public SettingsBase Settings { get; }
    }
}
