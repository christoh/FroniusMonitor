using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using De.Hochstaetter.Fronius.Services;

namespace De.Hochstaetter.Fronius.Contracts
{
    public interface IWattPilotService
    {
        WebConnection? Connection { get; }
        WattPilot? WattPilot { get; }
        IReadOnlyList<WattPilotAcknowledge> UnsuccessfulWrites { get; }
        ValueTask Start(WebConnection connection);
        ValueTask Stop();
        Task WaitSendValues(int timeout = 5000);
        void BeginSendValues();
        ValueTask SendValue(WattPilot instance, string propertyName);
        ValueTask<List<string>> Send(WattPilot? localWattPilot = null, WattPilot? oldWattPilot = null);
    }
}
