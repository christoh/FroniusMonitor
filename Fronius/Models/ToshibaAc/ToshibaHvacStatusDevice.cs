namespace De.Hochstaetter.Fronius.Models.ToshibaAc
{
    public class ToshibaHvacStatusDevice : ToshibaHvacDeviceBase
    {
        private Guid acId;

        [JsonPropertyName("ACId")]
        public Guid AcId
        {
            get => acId;
            set => Set(ref acId, value);
        }

        private Guid id;

        [JsonPropertyName("Id")]
        public Guid Id
        {
            get => id;
            set => Set(ref id, value);
        }

        private ToshibaHvacPowerState powerState;

        [JsonPropertyName("OnOff")]
        public ToshibaHvacPowerState PowerState
        {
            get => powerState;
            set => Set(ref powerState, value, () => NotifyOfPropertyChange(nameof(IsTurnedOn)));
        }

        private Guid deviceUniqueId;

        [JsonPropertyName("ACDeviceUniqueId")]
        public Guid DeviceUniqueId
        {
            get => deviceUniqueId;
            set => Set(ref deviceUniqueId, value);
        }

        private string versionInfo = string.Empty;
        [JsonPropertyName("VersionInfo")]
        public string VersionInfo
        {
            get => versionInfo;
            set => Set(ref versionInfo, value);
        }

        private int acModelId;

        [JsonPropertyName("Model")]
        public int AcModelId
        {
            get => acModelId;
            set => Set(ref acModelId, value);
        }

        private DateTime updatedDate;
        [JsonPropertyName("UpdatedDate")]
        public DateTime UpdatedDate
        {
            get => updatedDate;
            set => Set(ref updatedDate, value);
        }

        private double latitude;
        [JsonPropertyName("Lat")]
        public double Latitude
        {
            get => latitude;
            set => Set(ref latitude, value);
        }

        private double longitude;
        [JsonPropertyName("Long")]
        public double Longitude
        {
            get => longitude;
            set => Set(ref longitude, value);
        }

        private bool isMapped;
        [JsonPropertyName("IsMapped")]
        public bool IsMapped
        {
            get => isMapped;
            set => Set(ref isMapped, value);
        }

        private DateTime firstConnectionTime;
        [JsonPropertyName("FirstConnectionTime")]
        public DateTime FirstConnectionTime
        {
            get => firstConnectionTime;
            set => Set(ref firstConnectionTime, value);
        }

        private DateTime lastConnectionTime;
        [JsonPropertyName("LastConnectionTime")]
        public DateTime LastConnectionTime
        {
            get => lastConnectionTime;
            set => Set(ref lastConnectionTime, value);
        }

        private string consumerMasterId = string.Empty;
        [JsonPropertyName("ConsumerMasterId")]
        public string ConsumerMasterId
        {
            get => consumerMasterId;
            set => Set(ref consumerMasterId, value);
        }

        private Guid partitionKey;
        [JsonPropertyName("PartitionKey")]
        public Guid PartitionKey
        {
            get => partitionKey;
            set => Set(ref partitionKey, value);
        }
    }
}
