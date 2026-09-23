using System.Runtime.CompilerServices;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using De.Hochstaetter.HomeAutomationServer.Models.Settings;
using De.Hochstaetter.HomeAutomationServer.Services;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;
using Microsoft.Extensions.Options;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// What a bearer token stands for, and every way it stops standing for it: expiry, logout, a deleted user and a
/// changed password - plus the <c>Authentication</c> element of Settings.xml that decides its lifetime.
/// </summary>
public sealed class BearerTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly User bob = TestUsers.Create("bob", Roles.User);
    private readonly SettableTimeProvider clock = new(Now);
    private readonly UserList userList = new();
    private readonly BearerTokenService tokens;

    public BearerTokenServiceTests()
    {
        userList.Users = [bob];
        tokens = new BearerTokenService(new StaticOptionsMonitor(userList), clock, NullLogger<BearerTokenService>.Instance);
    }

    [Fact]
    public void A_token_stands_for_the_user_it_was_issued_for()
    {
        var token = tokens.Issue(bob);

        Assert.Same(bob, tokens.Validate(token.Value));
        Assert.Equal(TimeSpan.FromMinutes(AuthenticationSettings.DefaultBearerTokenLifetimeMinutes), token.Lifetime);
    }

    [Fact]
    public void Every_token_is_a_different_one()
    {
        Assert.NotEqual(tokens.Issue(bob).Value, tokens.Issue(bob).Value);
    }

    [Fact]
    public void A_token_nobody_issued_stands_for_nobody()
    {
        tokens.Issue(bob);

        Assert.Null(tokens.Validate("made up"));
        Assert.Null(tokens.Validate(""));
        Assert.Null(tokens.Validate(null));
    }

    [Fact]
    public void A_token_expires_after_the_configured_lifetime()
    {
        userList.Authentication.BearerTokenLifetimeMinutes = 5;
        var token = tokens.Issue(bob);

        clock.Now = Now + TimeSpan.FromMinutes(5) - TimeSpan.FromSeconds(1);
        Assert.Same(bob, tokens.Validate(token.Value));

        clock.Now = Now + TimeSpan.FromMinutes(5);
        Assert.Null(tokens.Validate(token.Value));
    }

    [Fact]
    public void A_renewed_token_leaves_the_previous_one_valid_until_it_expires()
    {
        var first = tokens.Issue(bob);
        clock.Now = Now + TimeSpan.FromMinutes(28);
        var second = tokens.Issue(bob);

        Assert.Same(bob, tokens.Validate(first.Value));
        Assert.Same(bob, tokens.Validate(second.Value));

        clock.Now = Now + TimeSpan.FromMinutes(30);
        Assert.Null(tokens.Validate(first.Value));
        Assert.Same(bob, tokens.Validate(second.Value));
    }

    [Fact]
    public void A_revoked_token_stands_for_nobody()
    {
        var token = tokens.Issue(bob);

        Assert.Same(bob, tokens.Revoke(token.Value));
        Assert.Null(tokens.Validate(token.Value));
        Assert.Null(tokens.Revoke(token.Value));
    }

    [Fact]
    public void Changing_the_password_ends_every_session_of_the_user()
    {
        var token = tokens.Issue(bob);

        bob.SetPassword("something else");

        Assert.Null(tokens.Validate(token.Value));
    }

    [Fact]
    public void A_deleted_user_is_not_logged_in_any_more()
    {
        var token = tokens.Issue(bob);

        userList.Users.Remove(bob);

        Assert.Null(tokens.Validate(token.Value));
    }

    [Fact]
    public void A_new_user_of_the_same_name_does_not_inherit_the_session()
    {
        var token = tokens.Issue(bob);

        userList.Users.Remove(bob);
        userList.Users.Add(TestUsers.Create("bob", Roles.Administrator));

        Assert.Null(tokens.Validate(token.Value));
    }

    [Fact]
    public void A_renamed_user_stays_logged_in()
    {
        var token = tokens.Issue(bob);

        bob.Username = "robert";

        Assert.Same(bob, tokens.Validate(token.Value));
    }

    [Fact]
    public void The_guest_gets_a_token_like_anybody_else()
    {
        var token = tokens.Issue(User.Guest);

        Assert.Same(User.Guest, tokens.Validate(token.Value));

        userList.EnableGuestAccount = false;
        Assert.Null(tokens.Validate(token.Value));
    }

    [Fact]
    public void A_lifetime_below_one_minute_is_taken_as_one_minute()
    {
        userList.Authentication.BearerTokenLifetimeMinutes = 0;

        Assert.Equal(TimeSpan.FromMinutes(1), tokens.Issue(bob).Lifetime);
    }

    [Fact]
    public void The_settings_are_in_the_file_with_the_debugging_schemes_off()
    {
        var xml = SettingsXml.Serialize(new Settings());

        // Written even when they hold their defaults, so that what a server accepts is readable from Settings.xml.
        Assert.Contains("<BearerTokenLifetimeMinutes>30</BearerTokenLifetimeMinutes>", xml);
        Assert.Contains("<EnableBasicAuthentication>false</EnableBasicAuthentication>", xml);
        Assert.Contains("<EnableCookieAuthentication>false</EnableCookieAuthentication>", xml);
        Assert.DoesNotContain("<BearerTokenLifetime>", xml);
    }

    [Fact]
    public void The_settings_round_trip_and_a_file_that_predates_them_gets_the_defaults()
    {
        var read = SettingsXml.Deserialize(SettingsXml.Serialize(new Settings
        {
            Authentication = new AuthenticationSettings { BearerTokenLifetimeMinutes = 7, EnableBasicAuthentication = true, EnableCookieAuthentication = true },
        })).Authentication;

        Assert.Equal(7, read.BearerTokenLifetimeMinutes);
        Assert.True(read.EnableBasicAuthentication);
        Assert.True(read.EnableCookieAuthentication);

        var old = SettingsXml.Deserialize("<Settings><ServerPort>1502</ServerPort></Settings>").Authentication;
        Assert.Equal(AuthenticationSettings.DefaultBearerTokenLifetimeMinutes, old.BearerTokenLifetimeMinutes);
        Assert.False(old.EnableBasicAuthentication);
        Assert.False(old.EnableCookieAuthentication);
    }

    /// <summary>
    /// The example file is what an administrator copies, so it has to load, and what it says about authentication
    /// has to be what a server without the element does anyway.
    /// </summary>
    [Fact]
    public void The_example_settings_file_shows_the_defaults()
    {
        var example = Settings.Load(Path.Combine(RepositoryRoot(), "HomeAutomationServer", "Settings.xml.example")).Authentication;
        var defaults = new AuthenticationSettings();

        Assert.Equal(defaults.BearerTokenLifetimeMinutes, example.BearerTokenLifetimeMinutes);
        Assert.Equal(defaults.EnableBasicAuthentication, example.EnableBasicAuthentication);
        Assert.Equal(defaults.EnableCookieAuthentication, example.EnableCookieAuthentication);
    }

    /// <summary>The checkout this source file is in: the example is not copied to the output of the tests.</summary>
    private static string RepositoryRoot([CallerFilePath] string thisFile = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", ".."));

    /// <summary>
    /// Always the same <see cref="UserList"/>, so a test can change it after the service was built - which is what
    /// the options Program.cs configures amount to, since they hand over the very objects the settings own.
    /// </summary>
    private sealed class StaticOptionsMonitor(UserList value) : IOptionsMonitor<UserList>
    {
        public UserList CurrentValue => value;

        public UserList Get(string? name) => value;

        public IDisposable? OnChange(Action<UserList, string?> listener) => null;
    }
}
