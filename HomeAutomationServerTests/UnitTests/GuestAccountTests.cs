using System.Xml.Serialization;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using De.Hochstaetter.HomeAutomationServer.Models.Settings;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The <c>EnableGuestAccount</c> element of Settings.xml and the one thing it decides: whether the name
/// <c>guest</c> means <see cref="User.Guest"/> or means nothing in particular.
/// </summary>
public sealed class GuestAccountTests
{
    [Fact]
    public void The_setting_is_in_the_file_whichever_way_it_is_set()
    {
        // Written even when it holds its default, so that a server's answer to this is in Settings.xml and not
        // implied by the absence of an element nobody knew to look for.
        Assert.Contains("<EnableGuestAccount>true</EnableGuestAccount>", Serialize(new Settings()));
        Assert.Contains("<EnableGuestAccount>false</EnableGuestAccount>", Serialize(new Settings { EnableGuestAccount = false }));
    }

    [Fact]
    public void The_setting_round_trips_and_a_file_that_predates_it_keeps_the_account()
    {
        Assert.False(Deserialize(Serialize(new Settings { EnableGuestAccount = false })).EnableGuestAccount);
        Assert.True(Deserialize(Serialize(new Settings())).EnableGuestAccount);

        // A Settings.xml written before the element existed reads as the behaviour that file had.
        Assert.True(Deserialize("<Settings><ServerPort>1502</ServerPort></Settings>").EnableGuestAccount);
    }

    [Fact]
    public void The_guest_is_found_although_no_user_list_holds_it()
    {
        var users = new UserList { Users = [TestUsers.Create("bob", Roles.User)] };

        Assert.Same(User.Guest, users.Find("guest"));
        Assert.Same(User.Guest, users.Find("GUEST"));
        Assert.True(users.IsBuiltInGuest("Guest"));
    }

    [Fact]
    public void The_guest_is_nobody_once_the_account_is_switched_off()
    {
        var users = new UserList { Users = [TestUsers.Create("bob", Roles.User)], EnableGuestAccount = false };

        Assert.Null(users.Find("guest"));
        Assert.False(users.IsBuiltInGuest("guest"));
    }

    /// <summary>
    /// With the account off the name stops being reserved, so an administrator may create, rename and delete a
    /// user called <c>guest</c> like any other - which is the whole point of being able to switch it off.
    /// </summary>
    [Fact]
    public void A_user_named_guest_is_an_ordinary_user_once_the_account_is_switched_off()
    {
        var namesake = TestUsers.Create("Guest", Roles.Administrator);
        var users = new UserList { Users = [namesake], EnableGuestAccount = false };

        Assert.Same(namesake, users.Find("guest"));
    }

    /// <summary>
    /// The other way round it is the built-in guest that wins, and the namesake cannot log in at all. That is why
    /// <c>Program.LogGuestAccount</c> warns about such a user at startup instead of leaving them to wonder.
    /// </summary>
    [Fact]
    public void The_guest_hides_a_user_of_the_same_name_while_the_account_is_on()
    {
        var users = new UserList { Users = [TestUsers.Create("guest", Roles.Administrator)] };

        Assert.Same(User.Guest, users.Find("guest"));
    }

    [Fact]
    public void A_user_list_has_the_account_on_until_something_switches_it_off()
    {
        Assert.True(new UserList().EnableGuestAccount);
    }

    [Fact]
    public void An_unknown_name_and_no_name_at_all_find_nobody()
    {
        var users = new UserList { Users = [TestUsers.Create("bob", Roles.User)] };

        Assert.Null(users.Find("mallory"));
        Assert.Null(users.Find(null));
    }

    private static string Serialize(Settings settings)
    {
        using var writer = new StringWriter();
        new XmlSerializer(typeof(Settings)).Serialize(writer, settings);
        return writer.ToString();
    }

    private static Settings Deserialize(string xml)
    {
        using var reader = new StringReader(xml);
        return (Settings)new XmlSerializer(typeof(Settings)).Deserialize(reader)!;
    }
}
