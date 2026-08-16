using System;
using System.Collections.Generic;
using System.Text;

namespace De.Hochstaetter.HomeAutomationClient.Contracts;

public interface IKeyedDevice
{
    string Key { get; }
    object Blob { get; }
}
