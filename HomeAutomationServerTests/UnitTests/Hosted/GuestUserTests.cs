using System.Net;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.Services;
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

[CollectionDefinition("Settings", DisableParallelization = true)]
public class SettingsCollection;

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
        builder.Services.Configure<UserList>(u => u.Users = settings.Users);
        builder.Services.AddControllers().AddApplicationPart(typeof(IdentityController).Assembly);
        builder.Services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, BasicAuthenticationService>("Basic", null);

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

        Assert.DoesNotContain(settings.Users, u => u.Username == "guest");
        
        if (File.Exists(settingsFile))
        {
             var loadedSettings = Settings.Load(settingsFile);
             Assert.DoesNotContain(loadedSettings.Users, u => u.Username == "guest");
        }
    }

    [Fact]
    public async Task Guest_cannot_be_deleted()
    {
        var adminUser = new User { Username = "admin", Roles = Roles.Administrator };
        adminUser.SetPassword("password");
        settings.Users.Add(adminUser);

        using var admin = new WebClientService();
        admin.Initialize(apiUri, "test", "1.0");
        await admin.Login("admin", "password");

        var result = await admin.DeleteUser("guest");
        
        Assert.Equal(HttpStatusCode.UnprocessableEntity, result.Status);
        Assert.DoesNotContain(settings.Users, u => u.Username == "guest");
    }

    [Fact]
    public async Task Guest_cannot_be_renamed_or_modified()
    {
        var adminUser = new User { Username = "admin", Roles = Roles.Administrator };
        adminUser.SetPassword("password");
        settings.Users.Add(adminUser);

        using var admin = new WebClientService();
        admin.Initialize(apiUri, "test", "1.0");
        await admin.Login("admin", "password");

        var result = await admin.UpdateUser("guest", new UserAccount { UserName = "intruder", Roles = Roles.Administrator });
        
        Assert.Equal(HttpStatusCode.UnprocessableEntity, result.Status);
        Assert.Equal("guest", User.Guest.Username);
        Assert.Equal(Roles.Guest, User.Guest.Roles);
    }
}
