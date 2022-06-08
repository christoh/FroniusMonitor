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
        Task Start(WebConnection connection);
        Task Stop();
    }
}
