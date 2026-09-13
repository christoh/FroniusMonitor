using De.Hochstaetter.HomeAutomationServer;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using De.Hochstaetter.HomeAutomationServer.Models.Settings;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// That a server always starts with somebody who can administer it, or does not start at all.
/// </summary>
/// <remarks>
/// Every endpoint that creates or repairs a user asks for <see cref="Roles.Administrator"/>, so a
/// <c>Settings.xml</c> with users but no administrator is a state no client can get out of. The two cases have to
/// stay apart: <em>no users at all</em> is a fresh installation and gets the default administrator, while
/// <em>users without an administrator</em> is a mistake that has to be said out loud instead of being papered over
/// with a second <c>admin</c> account that the real users know nothing about.
/// </remarks>
public class StartupAdministratorTests
{
    private readonly RecordingLogger logger = new();

    [Fact]
    public void EmptyUserListGetsTheDefaultAdministrator()
    {
        var settings = new Settings();

        Assert.Null(Program.EnsureAdministratorExists(settings, logger));

        var administrator = Assert.Single(settings.Users);
        Assert.Equal(Program.DefaultAdministratorName, administrator.Username);
        Assert.True(administrator.Roles.HasFlag(Roles.Administrator));
        Assert.True(administrator.Authenticate(Program.DefaultAdministratorPassword));
    }

    [Fact]
    public void TheDefaultAdministratorIsNotStoredInClearText()
    {
        var settings = new Settings();

        Program.EnsureAdministratorExists(settings, logger);

        var administrator = settings.Users.Single();
        Assert.NotEmpty(administrator.PasswordHash);
        Assert.NotEqual(Program.DefaultAdministratorPassword, administrator.PasswordHash);
        // The property that would write it out as it stands. It only ever reads back null.
        Assert.Null(administrator.ClearTextPassword);
    }

    [Fact]
    public void CreatingTheDefaultAdministratorIsLoggedAsAWarningNamingTheCredentials()
    {
        Program.EnsureAdministratorExists(new Settings(), logger);

        var (level, message) = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, level);
        // The point of the warning is that the reader can log in, so both halves have to be in it.
        Assert.Contains(Program.DefaultAdministratorName, message, StringComparison.Ordinal);
        Assert.Contains(Program.DefaultAdministratorPassword, message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(Roles.Administrator)]
    [InlineData(Roles.Administrator | Roles.User)]
    [InlineData(Roles.All)]
    public void AnExistingAdministratorIsLeftAlone(Roles roles)
    {
        var settings = new Settings { Users = [TestUsers.Create("someone", Roles.User), TestUsers.Create("boss", roles)] };

        Assert.Null(Program.EnsureAdministratorExists(settings, logger));

        Assert.Equal(2, settings.Users.Count);
        Assert.DoesNotContain(settings.Users, user => user.Username == Program.DefaultAdministratorName);
        Assert.Empty(logger.Entries);
    }

    [Theory]
    [InlineData(Roles.None)]
    [InlineData(Roles.User)]
    [InlineData(Roles.Guest | Roles.PowerUser | Roles.Operator | Roles.Developer)]
    public void UsersWithoutAnAdministratorStopTheServer(Roles roles)
    {
        var settings = new Settings { Users = [TestUsers.Create("someone", roles)] };

        Assert.Equal(Program.NoAdministratorExitCode, Program.EnsureAdministratorExists(settings, logger));

        var (level, _) = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, level);
    }

    [Fact]
    public void ARepairAdministratorIsNotSmuggledIntoAnExistingUserList()
    {
        // The tempting fix - add "admin" here as well - would hand a login to whoever reads the log, on a server
        // that already has accounts on it. Stopping is the answer; a human edits Settings.xml.
        var settings = new Settings { Users = [TestUsers.Create("someone", Roles.User)] };

        Program.EnsureAdministratorExists(settings, logger);

        Assert.Equal("someone", settings.Users.Single().Username);
    }

    /// <summary>
    /// <c>Program</c> configures the <see cref="UserList"/> options with <c>settings.Users</c> itself, and does so
    /// before this check runs. The authentication scheme therefore sees the created administrator only because the
    /// check <em>adds to</em> that set rather than putting a new one in its place.
    /// </summary>
    [Fact]
    public void TheCreatedAdministratorReachesTheAuthenticationScheme()
    {
        var settings = new Settings();
        var userDb = TestUsers.AsOptions();
        // What Program.cs does with settings.Users, before anybody is in it.
        userDb.CurrentValue.Users = settings.Users;

        Program.EnsureAdministratorExists(settings, logger);

        Assert.Equal(Program.DefaultAdministratorName, userDb.CurrentValue.Users.Single().Username);
    }
}
