using De.Hochstaetter.Fronius.Contracts;

namespace De.Hochstaetter.Fronius.Models
{
    public class DeviceInfo : BindableBase,IHaveDisplayName
    {
        public DeviceClass DeviceClass { get; init; }

        private string? serialNumber;

        public string? SerialNumber
        {
            get => serialNumber;
            set => Set(ref serialNumber, value);
        }

        public int Id { get; init; }

        public int DeviceType { get; init; }

        public virtual string Model
        {
            get => DeviceType.ToDeviceString();
            set { }
        }

        public virtual string DisplayName => $"{Model} #{Id}";
        public override string ToString() => DisplayName;
    }
}
