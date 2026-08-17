using System;
using System.Collections.Generic;
using System.Text;

namespace De.Hochstaetter.HomeAutomationClient.Models;

internal record SitePowerFlowUpdatedEventArgs(IKeyedDevice UpdatedDevice, Gen24PowerFlow SitePowerFlow);
