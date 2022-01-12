using De.Hochstaetter.Fronius.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Contracts
{
    public interface ISolarSystemService
    {
        SolarSystem? SolarSystem { get; }
        public double GridPowerSum { get; }
        public double LoadPowerSum { get; }
        public double SolarPowerSum { get; }
        public double StoragePowerSum { get; }
        public double AcPowerSum  { get; }
        public double DcPowerSum  { get; }
        public double PowerLossSum  { get; }
        public double? Efficiency  { get; }
        public double? GridPowerAvg  { get; }
        public double? LoadPowerAvg  { get; }
        public double? StoragePowerAvg { get; }
        public double? SolarPowerAvg  { get; }
        public double? PowerLossAvg { get; }

        event EventHandler<SolarDataEventArgs>? NewDataReceived;

        Task Start(WebConnection inverterConnection, WebConnection fritzBoxConnection);
        void Stop();
        void SuspendPowerConsumers();
        void ResumePowerConsumers();
    }
}
