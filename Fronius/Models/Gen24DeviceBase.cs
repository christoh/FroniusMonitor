using System.Diagnostics.CodeAnalysis;
using De.Hochstaetter.Fronius.Attributes;

namespace De.Hochstaetter.Fronius.Models
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public abstract class Gen24DeviceBase : BindableBase
    {
        private DateTime? dataTime;

        [Fronius("COMPONENTS_TIME_STAMP_U64")]
        public DateTime? DataTime
        {
            get => dataTime;
            set => Set(ref dataTime, value);
        }

        private string? manufacturer;

        [Fronius("manufacturer", FroniusDataType.Attribute)]
        public string? Manufacturer
        {
            get => manufacturer;
            set => Set(ref manufacturer, value);
        }

        private string? model;

        [Fronius("model", FroniusDataType.Attribute)]
        public string? Model
        {
            get => model;
            set => Set(ref model, value);
        }

        private bool? isEnabled;

        [Fronius("[ENABLE]", FroniusDataType.Attribute)]
        public bool? IsEnabled
        {
            get => isEnabled;
            set => Set(ref isEnabled, value);
        }

        private bool? isVisible;

        [Fronius("[VISIBLE]", FroniusDataType.Attribute)]
        public bool? IsVisible
        {
            get => isVisible;
            set => Set(ref isVisible, value);
        }

        private ushort? modbusAddress;
        [Fronius("addr", FroniusDataType.Attribute)]
        public ushort? ModbusAddress
        {
            get => modbusAddress;
            set => Set(ref modbusAddress, value);
        }

        private string? id;
        [Fronius("id", FroniusDataType.Attribute)]
        public string? Id
        {
            get => id;
            set => Set(ref id, value);
        }

        private string? busId;
        [Fronius("if",FroniusDataType.Attribute)]
        public string? BusId
        {
            get => busId;
            set => Set(ref busId, value);
        }

        private string? serialNumber;

        [Fronius("serial", FroniusDataType.Attribute)]
        public string? SerialNumber
        {
            get => serialNumber;
            set => Set(ref serialNumber, value);
        }

        private DateTime? creationTime;

        [Fronius("createTS", FroniusDataType.Attribute)]
        public DateTime? CreationTime
        {
            get => creationTime;
            set => Set(ref creationTime, value);
        }
    }
}
