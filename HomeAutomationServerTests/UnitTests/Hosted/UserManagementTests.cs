using System.Net;
using De.Hochstaetter.Fronius.Contracts.HomeAutomationClient;
using De.Hochstaetter.Fronius.Services.HomeAutomationClient;
using De.Hochstaetter.HomeAutomationServer.Controllers;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using De.Hochstaetter.HomeAutomationServer.Models.Settings;
using BasicAuthenticationService = De.Hochstaetter.HomeAutomationServer.Services.AuthenticationService;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

// Every call here goes to a loopback host and completes within a few milliseconds; a test that cannot be cancelled
// for that long costs nothing, and the tokens would only bury the assertions.
#pragma warning disable xUnit1051

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Hosted;

/// <summary>
/// The user endpoints of <see cref="IdentityController"/>, end to end: a Kestrel host with the real Basic
/// authentication, and the client's own <see cref="WebClientService"/> talking to it, so the JSON shapes of both
/// sides are proven against each other and not against a test's idea of them.
/// </summary>
public sealed class UserManagementTests : IAsyncLifetime
{
    private const string AdminName = "root";
    private const string MemberName = "bob";

    private readonly string settingsFile = Path.Combine(Path.GetTempPath(), $"UserManagementTests-{Guid.NewGuid():N}.xml");
    private readonly Settings settings = new() { Users = [TestUsers.Create(AdminName, Roles.Administrator), TestUsers.Create(MemberName, Roles.User)] };

    private WebApplication app = null!;
    private string apiUri = null!;
    private IWebClientService admin = null!;

    public async ValueTask InitializeAsync()
    {
        // SaveAsync without a name writes to this static, so it is pointed at a temp file. Nothing else in the test
        // assembly reads the static - the other settings tests pass their file names explicitly - which is what
        // makes this safe with xUnit running the classes in parallel.
        Settings.SettingsFileName = settingsFile;

        var builder = WebApplication.CreateBuilder();
        builder.Logging.AddTestOutput(LogLevel.Warning);
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        // The very wiring Program.cs has for the user store: one HashSet, reached as Settings and as UserList.
        builder.Services.AddSingleton(settings);
        builder.Services.Configure<UserList>(u => u.Users = settings.Users);
        builder.Services.AddControllers().AddApplicationPart(typeof(IdentityController).Assembly);
        builder.Services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, BasicAuthenticationService>("Basic", null);

        app = builder.Build();
        app.MapControllers();
        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        apiUri = address + "/api/";
        admin = await LoggedIn(AdminName, TestUsers.Password);
    }

    public async ValueTask DisposeAsync()
    {
        admin.Dispose();
        await app.DisposeAsync();
        File.Delete(settingsFile);
    }

    [Fact]
    public async Task An_administrator_sees_every_user_with_their_roles()
    {
        var result = await admin.GetUsers();

        Assert.Equal(HttpStatusCode.OK, result.Status);
        var users = Assert.IsType<List<UserInfo>>(result.Payload);
        Assert.Collection(users.OrderBy(u => u.UserName),
            u => { Assert.Equal(MemberName, u.UserName); Assert.Equal(Roles.User, u.Roles); },
            u => { Assert.Equal(AdminName, u.UserName); Assert.Equal(Roles.Administrator, u.Roles); });
    }

    [Fact]
    public async Task A_user_without_the_Administrator_role_is_refused()
    {
        using var member = await LoggedIn(MemberName, TestUsers.Password);

        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetUsers()).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.AddUser(new UserAccount { UserName = "mallory", Password = "x", Roles = Roles.Administrator })).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.UpdateUser(new UserAccount { UserName = MemberName, Roles = Roles.Administrator })).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.DeleteUser(AdminName)).Status);
        Assert.Equal(Roles.User, settings.Users.Single(u => u.Username == MemberName).Roles);
    }

    [Fact]
    public async Task A_new_user_is_added_persisted_and_can_log_in()
    {
        var result = await admin.AddUser(new UserAccount { UserName = "alice", Password = "wonderland", Roles = Roles.User | Roles.Operator });

        Assert.Equal(HttpStatusCode.OK, result.Status);
        Assert.Equal("alice", result.Payload?.UserName);
        Assert.Equal(Roles.User | Roles.Operator, result.Payload?.Roles);
        Assert.True(File.Exists(settingsFile));
        Assert.Contains("alice", await File.ReadAllTextAsync(settingsFile, TestContext.Current.CancellationToken));

        using var alice = new WebClientService();
        alice.Initialize(apiUri, "test", "1.0");
        var login = await alice.Login("alice", "wonderland");
        Assert.Equal(HttpStatusCode.OK, login.Status);
        Assert.Equal(Roles.User | Roles.Operator, login.Payload?.Roles);
    }

    [Fact]
    public async Task A_new_user_needs_a_password_and_an_unused_name()
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.AddUser(new UserAccount { UserName = "alice", Roles = Roles.User })).Status);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.AddUser(new UserAccount { UserName = MemberName.ToUpperInvariant(), Password = "x", Roles = Roles.User })).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.AddUser(new UserAccount { UserName = "", Password = "x", Roles = Roles.User })).Status);
        Assert.Equal(2, settings.Users.Count);
    }

    [Fact]
    public async Task Editing_changes_the_roles_and_only_a_given_password()
    {
        var rolesOnly = await admin.UpdateUser(new UserAccount { UserName = MemberName, Roles = Roles.PowerUser });
        Assert.Equal(HttpStatusCode.OK, rolesOnly.Status);
        Assert.Equal(Roles.PowerUser, rolesOnly.Payload?.Roles);

        using var bob = new WebClientService();
        bob.Initialize(apiUri, "test", "1.0");
        Assert.Equal(HttpStatusCode.OK, (await bob.Login(MemberName, TestUsers.Password)).Status);

        var withPassword = await admin.UpdateUser(new UserAccount { UserName = MemberName, Password = "new secret", Roles = Roles.PowerUser });
        Assert.Equal(HttpStatusCode.OK, withPassword.Status);

        Assert.Equal(HttpStatusCode.Unauthorized, (await bob.Login(MemberName, TestUsers.Password)).Status);
        Assert.Equal(HttpStatusCode.OK, (await bob.Login(MemberName, "new secret")).Status);
    }

    [Fact]
    public async Task An_unknown_user_cannot_be_edited_or_deleted()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await admin.UpdateUser(new UserAccount { UserName = "nobody", Roles = Roles.User })).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteUser("nobody")).Status);
    }

    [Fact]
    public async Task The_last_administrator_is_neither_demoted_nor_deleted()
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.UpdateUser(new UserAccount { UserName = AdminName, Roles = Roles.User })).Status);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.DeleteUser(AdminName)).Status);
        Assert.Equal(Roles.Administrator, settings.Users.Single(u => u.Username == AdminName).Roles);

        // With a second administrator in place the first one may go.
        Assert.Equal(HttpStatusCode.OK, (await admin.UpdateUser(new UserAccount { UserName = MemberName, Roles = Roles.Administrator })).Status);
        Assert.Equal(HttpStatusCode.OK, (await admin.DeleteUser(AdminName)).Status);
        Assert.DoesNotContain(settings.Users, u => u.Username == AdminName);
    }

    [Fact]
    public async Task A_deleted_user_is_gone_and_cannot_log_in()
    {
        var result = await admin.DeleteUser(MemberName);

        Assert.Equal(HttpStatusCode.OK, result.Status);
        Assert.True(result.Payload);
        Assert.DoesNotContain(settings.Users, u => u.Username == MemberName);

        using var bob = new WebClientService();
        bob.Initialize(apiUri, "test", "1.0");
        Assert.Equal(HttpStatusCode.Unauthorized, (await bob.Login(MemberName, TestUsers.Password)).Status);
    }

    private async Task<IWebClientService> LoggedIn(string userName, string password)
    {
        var client = new WebClientService();
        client.Initialize(apiUri, "test", "1.0");
        var login = await client.Login(userName, password);
        Assert.Equal(HttpStatusCode.OK, login.Status);
        return client;
    }
}