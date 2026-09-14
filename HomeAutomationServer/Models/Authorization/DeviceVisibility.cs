using System.Security.Claims;
using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.Fronius.Models.Modbus;

namespace De.Hochstaetter.HomeAutomationServer.Models.Authorization;

/// <summary>
/// Which devices reach a client that holds no more than <see cref="Roles.Guest"/>. One list, asked by the hub when
/// it greets a connection and by <c>SignalRDispatcher</c> on every push, and an allow list on purpose: a device
/// type added later stays hidden from guests until somebody puts it here.
/// </summary>
/// <remarks>
/// The controllers say the same thing in their own words: the read endpoints of <c>Gen24Controller</c> carry
/// <c>Roles = "User,Guest"</c>, every other controller stays with <c>User</c>. Change both together.
/// </remarks>
public static class DeviceVisibility
{
    /// <summary>
    /// Inverters - a Gen24 carries its smart meter and its battery inside - and the SunSpec inverters and meters.
    /// Not the Fritz!Box outlets, the air conditioners, the chargers, nor the price data.
    /// </summary>
    public static bool IsVisibleToGuests(object device) => device is Gen24System or SunSpecInverter or SunSpecMeter;

    public static bool IsVisibleTo(object device, ClaimsPrincipal? user) => (user?.SeesAllDevices() ?? false) || IsVisibleToGuests(device);
}
