using De.Hochstaetter.Fronius.Attributes;
using De.Hochstaetter.Fronius.Localization;

namespace De.Hochstaetter.Fronius.Models.Gen24
{
    public class Gen24Status : BindableBase
    {
        private uint? id;
        [FroniusProprietaryImport("id", FroniusDataType.Root)]
        public uint? Id
        {
            get => id;
            set => Set(ref id, value);
        }

        private DeviceType deviceType;
        [FroniusProprietaryImport("type", FroniusDataType.Root)]
        public DeviceType DeviceType
        {
            get => deviceType;
            set => Set(ref deviceType, value);
        }

        private string? statusCode;
        [FroniusProprietaryImport("statusMessage", FroniusDataType.Root)]
        public string? StatusCode
        {
            get => statusCode;
            set => Set(ref statusCode, value, () => NotifyOfPropertyChange(nameof(StatusMessage)));
        }

        private byte? status;
        [FroniusProprietaryImport("status", FroniusDataType.Root)]
        public byte? Status
        {
            get => status;
            set => Set(ref status, value);
        }

        public string? StatusMessage => Resources.ResourceManager.GetString(StatusCode ?? "STATE_UNKNOWN");
    }
}
