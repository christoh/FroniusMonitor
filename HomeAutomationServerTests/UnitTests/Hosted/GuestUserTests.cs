using System.Net;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.Services;
using De.Hochstaetter.HomeAutomationServer.Controllers;
using De.Hochstaetter.HomeAutomationServer.Hubs;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using De.Hochstaetter.HomeAutomationServer.Models.Settings;
using De.Hochstaetter.HomeAutomationServer.Services;
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

[Collection("Settings")]
public sealed class GuestUserTests : IAsyncLifetime
{
    private readonly string settingsFile = Path.Combine(Path.GetTempPath(), $"GuestUserTests-{Guid.NewGuid():N}.xml");
    private readonly Settings settings = new() { Users = [] };

    private WebApplication app = null!;
    private string apiUri = null!;

    public async ValueTask InitializeAsync()
    {
        Settings.SettingsFileName = settingsFile;

        var builder = WebApplication.CreateBuilder();
        builder.Logging.AddTestOutput(LogLevel.Warning);
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton<IAesKeyProvider>(new TestAesKeyProvider());
        builder.Services.Configure<UserList>(u =>
        {
            u.Users = settings.Users;
            u.EnableGuestAccount = settings.EnableGuestAccount;
        });
        builder.Services.AddControllers().AddApplicationPart(typeof(IdentityController).Assembly);
        builder.Services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, BasicAuthenticationService>("Basic", null);
        builder.Services.AddHubTicketAuthentication();

        app = builder.Build();
        app.MapControllers();
        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        apiUri = address + "/api/";
    }

    public async ValueTask DisposeAsync()
    {
        await app.DisposeAsync();
        if (File.Exists(settingsFile))
        {
            File.Delete(settingsFile);
        }
    }

    [Fact]
    public async Task Guest_can_log_in_with_password_guest()
    {
        using var guest = new WebClientService();
        guest.Initialize(apiUri, "test", "1.0");
        
        var login = await guest.Login("guest", "guest");
        
        Assert.Equal(HttpStatusCode.OK, login.Status);
        Assert.Equal("guest", login.Payload?.UserName);
        Assert.Equal(Roles.Guest, login.Payload?.Roles);
    }

    [Fact]
    public async Task Guest_is_not_in_settings_even_after_login()
    {
        using var guest = new WebClientService();
        guest.Initialize(apiUri, "test", "1.0");
        await guest.Login("guest", "guest");

        Assert.DoesNotContain(settings.Users, u => User.IsGuest(u.Username));

        // Nothing was written at all, which says more than reading the file back would: a login that saved the
        // settings could only have saved the guest into them.
        Assert.False(File.Exists(settingsFile));
    }

    [Fact]
    public async Task Guest_cannot_be_deleted()
    {
        using var admin = await LoggedInAdministrator();

        var result = await admin.DeleteUser("guest");
        
        Assert.Equal(HttpStatusCode.UnprocessableEntity, result.Status);
        Assert.DoesNotContain(settings.Users, u => u.Username == "guest");
    }

    [Fact]
    public async Task Guest_cannot_be_renamed_or_modified()
    {
        using var admin = await LoggedInAdministrator();

        var result = await admin.UpdateUser("guest", new UserAccount { UserName = "intruder", Roles = Roles.Administrator });
        
        Assert.Equal(HttpStatusCode.UnprocessableEntity, result.Status);
        Assert.Equal("guest", User.Guest.Username);
        Assert.Equal(Roles.Guest, User.Guest.Roles);
    }

    /// <summary>
    /// The endpoint that needs no Administrator role, so the guest can reach it itself - and there is exactly one
    /// <see cref="User.Guest"/> in the process, so a password change here would be a password change for every
    /// other guest until the server is restarted, saved nowhere and undoable by nobody.
    /// </summary>
    [Fact]
    public async Task The_password_of_the_guest_cannot_be_changed()
    {
        using var guest = new WebClientService();
        guest.Initialize(apiUri, "test", "1.0");
        await guest.Login("guest", "guest");

        var result = await guest.ChangePassword(new ChangePasswordRequest { CurrentPassword = "guest", NewPassword = "something else" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, result.Status);
        Assert.True(User.Guest.Authenticate("guest"));
        Assert.False(User.Guest.Authenticate("something else"));
    }

    /// <summary>
    /// A ticket the hub then refuses is worse than no ticket at all: the guest logs in, the client connects, and
    /// nothing ever arrives. <see cref="HubTicketService.Validate"/> looks the name up in the user list, which is
    /// the one place the guest is not.
    /// </summary>
    [Fact]
    public async Task The_hub_ticket_of_the_guest_is_one_the_hub_accepts()
    {
        using var guest = new WebClientService();
        guest.Initialize(apiUri, "test", "1.0");
        await guest.Login("guest", "guest");

        var ticket = await guest.GetHubTicket();

        Assert.Same(User.Guest, app.Services.GetRequiredService<HubTicketService>().Validate(ticket));
    }

    [Fact]
    public async Task Guest_cannot_be_added_as_a_user_of_its_own()
    {
        using var admin = await LoggedInAdministrator();

        var result = await admin.AddUser(new UserAccount { UserName = "Guest", Password = "something else", Roles = Roles.Administrator });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, result.Status);
        Assert.DoesNotContain(settings.Users, u => User.IsGuest(u.Username));
    }

    /// <summary>
    /// The guest is not in the list an administrator is shown, because there is nothing about it to administer:
    /// every endpoint that changes a user refuses it.
    /// </summary>
    [Fact]
    public async Task An_administrator_is_not_shown_the_guest_among_the_users()
    {
        using var admin = await LoggedInAdministrator();

        var result = await admin.GetUsers();

        Assert.Equal(HttpStatusCode.OK, result.Status);
        Assert.DoesNotContain(result.Payload!, u => User.IsGuest(u.UserName));
    }

    private async Task<IWebClientService> LoggedInAdministrator()
    {
        settings.Users.Add(TestUsers.Create("admin", Roles.Administrator));

        var admin = new WebClientService();
        admin.Initialize(apiUri, "test", "1.0");
        await admin.Login("admin", TestUsers.Password);
        return admin;
    }
}
