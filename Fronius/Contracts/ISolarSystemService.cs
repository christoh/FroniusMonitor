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
        event EventHandler<SolarDataEventArgs>? NewDataReceived;

        Task Start(WebConnection inverterConnection, WebConnection fritzBoxConnection);
        void Stop();
        void SuspendPowerConsumers();
        void ResumePowerConsumers();
    }
}
