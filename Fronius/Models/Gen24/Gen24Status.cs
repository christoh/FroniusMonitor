namespace De.Hochstaetter.Fronius.Models.Gen24
{
    public class Gen24Status : BindableBase
    {
        private static readonly IGen24Service? gen24Service = IoC.TryGetRegistered<IGen24Service>();

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
            set => Set(ref deviceType, value, () =>
            {
                NotifyOfPropertyChange(nameof(StatusMessage));
                NotifyOfPropertyChange(nameof(StatusMessageCaption));
            });
        }

        private string? statusCode;
        [FroniusProprietaryImport("statusMessage", FroniusDataType.Root)]
        public string? StatusCode
        {
            get => statusCode;
            set => Set(ref statusCode, value, () =>
            {
                NotifyOfPropertyChange(nameof(StatusMessage));
                NotifyOfPropertyChange(nameof(StatusMessageCaption));
            });
        }

        private byte? status;
        [FroniusProprietaryImport("status", FroniusDataType.Root)]
        public byte? Status
        {
            get => status;
            set => Set(ref status, value);
        }

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        public string? StatusMessage => gen24Service?.GetUiString((DeviceType == DeviceType.PowerMeter ? "POWERMETER" : "INVERTER") + ".DEVICESTATE." + StatusCode).GetAwaiter().GetResult();

        public string? StatusMessageCaption => Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName == "en" ? StatusMessage?[0].ToString().ToUpperInvariant() + StatusMessage?[1..] : StatusMessage;
    }
}
