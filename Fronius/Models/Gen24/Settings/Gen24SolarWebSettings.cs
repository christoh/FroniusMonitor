namespace De.Hochstaetter.Fronius.Models.Gen24.Settings
{
    public class Gen24SolarWebSettings : BindableBase, ICloneable
    {
        private bool customerRemoteControlProfileActive;
        [FroniusProprietaryImport("customerRemoteControlProfileActive",  FroniusDataType.Root)]
        public bool CustomerRemoteControlProfileActive
        {
            get => customerRemoteControlProfileActive;
            set => Set(ref customerRemoteControlProfileActive, value);
        }

        private bool enableRemoteControl;
        [FroniusProprietaryImport("enableRemoteControl",  FroniusDataType.Root)]
        public bool EnableRemoteControl
        {
            get => enableRemoteControl;
            set => Set(ref enableRemoteControl, value);
        }

        private bool sendDataToSolarWeb;
        [FroniusProprietaryImport("sendDataToSolarWeb",  FroniusDataType.Root)]
        public bool SendDataToSolarWeb
        {
            get => sendDataToSolarWeb;
            set => Set(ref sendDataToSolarWeb, value);
        }

        private bool technicianRemoteControlProfileActive;
        [FroniusProprietaryImport("technicianRemoteControlProfileActive",  FroniusDataType.Root)]
        public bool TechnicianRemoteControlProfileActive
        {
            get => technicianRemoteControlProfileActive;
            set => Set(ref technicianRemoteControlProfileActive, value);
        }

        public object Clone() => MemberwiseClone();
    }
}
