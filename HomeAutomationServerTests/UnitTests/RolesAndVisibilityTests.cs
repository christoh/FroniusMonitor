using System.Security.Claims;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.Fronius.Models.EnergyData;
using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.Fronius.Models.ToshibaAc;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The line between a guest's view and a user's: which roles draw it, and which devices fall on which side.
/// </summary>
public sealed class RolesAndVisibilityTests
{
    [Theory]
    [InlineData(Roles.None, false)]
    [InlineData(Roles.Guest, false)]
    [InlineData(Roles.Guest | Roles.PowerUser | Roles.Operator, false)]
    [InlineData(Roles.User, true)]
    [InlineData(Roles.Administrator, true)]
    [InlineData(Roles.Guest | Roles.User, true)]
    public void Only_users_and_administrators_see_everything(Roles roles, bool seesAll)
    {
        Assert.Equal(seesAll, roles.SeesAllDevices());
    }

    [Fact]
    public void A_guest_sees_inverters_and_nothing_that_is_switched_charged_or_paid_for()
    {
        Assert.True(DeviceVisibility.IsVisibleToGuests(new Gen24System()));

        Assert.False(DeviceVisibility.IsVisibleToGuests(new FritzBoxDevice()));
        Assert.False(DeviceVisibility.IsVisibleToGuests(new WattPilot()));
        Assert.False(DeviceVisibility.IsVisibleToGuests(new WattPilotUpdate("424242", "{}")));
        Assert.False(DeviceVisibility.IsVisibleToGuests(new ToshibaHvacMappingDevice()));
        Assert.False(DeviceVisibility.IsVisibleToGuests(new EnergyChartData()));
    }

    [Fact]
    public void A_user_sees_what_a_guest_does_not()
    {
        var outlet = new FritzBoxDevice();

        Assert.True(DeviceVisibility.IsVisibleTo(outlet, Principal(Roles.User)));
        Assert.True(DeviceVisibility.IsVisibleTo(outlet, Principal(Roles.Administrator)));
        Assert.False(DeviceVisibility.IsVisibleTo(outlet, Principal(Roles.Guest)));
        Assert.False(DeviceVisibility.IsVisibleTo(outlet, null));
        Assert.True(DeviceVisibility.IsVisibleTo(new Gen24System(), Principal(Roles.Guest)));
    }

    [Fact]
    public void The_roles_come_back_out_of_the_claims_the_ticket_put_in()
    {
        Assert.Equal(Roles.Guest | Roles.Operator, Principal(Roles.Guest | Roles.Operator).GetRoles());
        Assert.Equal(Roles.None, new ClaimsPrincipal(new ClaimsIdentity()).GetRoles());
    }

    /// <summary>The principal every scheme of the server builds for a user with these roles.</summary>
    private static ClaimsPrincipal Principal(Roles roles) => TestUsers.Create("someone", roles).CreateAuthenticationTicket("Basic").Principal;
}
