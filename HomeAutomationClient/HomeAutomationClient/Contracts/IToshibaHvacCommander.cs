using De.Hochstaetter.Fronius.Models.ToshibaAc;

namespace De.Hochstaetter.HomeAutomationClient.Contracts;

/// <summary>
/// Sends a command to one or more Toshiba air conditioners through the server. Split off <see cref="IUpdateService"/>
/// so that <see cref="ToshibaHvacViewModel"/> depends on this alone and a test can stand in for it - the update
/// service's interface has an internal member and cannot be implemented outside the client.
/// </summary>
public interface IToshibaHvacCommander
{
    /// <summary>
    /// Over the hub: the server sends the command through its IoT Hub connection and waits for each air
    /// conditioner to echo it. The result names the targets that stayed silent; the caller shows an error for those.
    /// Needs the PowerUser role.
    /// </summary>
    /// <param name="ids">The ids the devices are published under (the keys of <see cref="IUpdateService.AllPowerConsumers"/>).</param>
    /// <param name="state">The bytes to change; every other byte stays 0xff, "leave it".</param>
    Task<ToshibaHvacCommandResult> SendToshibaHvacCommand(string[] ids, ToshibaHvacStateData state);
}
