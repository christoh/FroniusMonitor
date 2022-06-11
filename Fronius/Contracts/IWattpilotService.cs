using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Contracts
{
    public interface IWattPilotService
    {
        WebConnection? Connection { get; }
        WattPilot? WattPilot { get; }
        ValueTask Start(WebConnection connection);
        ValueTask Stop();
        Task WaitSendValues(int timeout = 5000);
        void BeginSendValues();
        ValueTask SendValue(WattPilot instance, string propertyName);
    }
}
