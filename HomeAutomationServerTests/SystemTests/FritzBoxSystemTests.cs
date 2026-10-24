using System.Diagnostics;
using System.Reflection;
using System.Xml.Serialization;
using De.Hochstaetter.FritzBox.Http;
using De.Hochstaetter.FritzBox.SmartHome;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Settings;

namespace De.Hochstaetter.HomeAutomationServerTests.SystemTests;

/// <summary>
/// The FritzBox library against the real FRITZ!Box in Neufahrn (FRITZ!OS 8.25, 17 smart home units: 13 outlets and 4
/// lamps when it was measured), by default over the LAN at <c>http://192.168.44.11</c>. From elsewhere, the cloud
/// included, <c>FRITZBOX_URL=https://fritzbox.hochstaetter.de</c> reaches the same box through nginx on AWS, with a
/// certificate the system trusts.
/// </summary>
/// <remarks>
/// <para>
/// The credentials are not in the code. The environment of the process that runs the tests (a terminal or the IDE has
/// to be restarted after the variable is set) holds them:
/// </para>
/// <list type="bullet">
/// <item><c>FRITZBOX_URL</c>: the box's web interface. Unset means <c>http://192.168.44.11</c>.</item>
/// <item><c>FRITZBOX_PASSWORD</c>: the password of the box user. Required by every test that logs in; the one that does
/// not (the version) works without it.</item>
/// <item><c>FRITZBOX_USER</c>: the box user. Empty or unset means the user the box names as the last one to log in.</item>
/// <item><c>FRITZBOX_TEST_AIN</c>: the AIN of an outlet that <see cref="An_outlet_named_by_FRITZBOX_TEST_AIN_is_switched_to_the_opposite_state_and_back"/>
/// may switch. Without it, that test is skipped. Nothing else here switches anything.</item>
/// </list>
/// <para>
/// All of them are explicit, so a plain <c>dotnet test</c> leaves them alone. To run them:
/// <c>dotnet test --project HomeAutomationServerTests/HomeAutomationServerTests.csproj -c Debug -- --explicit only
/// --filter-class "*FritzBoxSystemTests"</c>.
/// </para>
/// <para>
/// Every test that logs in opens a session of its own and does not log it out (the library has no logout): the box
/// lets it expire. Apart from the one switching test and the routine test, which say so, the tests only read.
/// </para>
/// </remarks>
public sealed class FritzBoxSystemTests(ITestOutputHelper output) : IDisposable
{
    private const string UrlVariable = "FRITZBOX_URL";
    private const string LanUrl = "http://192.168.44.11";
    private const string PasswordVariable = "FRITZBOX_PASSWORD";
    private const string UserVariable = "FRITZBOX_USER";
    private const string TestAinVariable = "FRITZBOX_TEST_AIN";

    private static readonly Version restApiVersion = new(8, 20);

    /// <summary>The box's web interface, from <c>FRITZBOX_URL</c> or the LAN address of the box in Neufahrn.</summary>
    private static string BoxUrl => Environment.GetEnvironmentVariable(UrlVariable) is { Length: > 0 } url ? url.TrimEnd('/') : LanUrl;
    private static readonly TimeSpan pollInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan pollTimeout = TimeSpan.FromSeconds(10);
    private static readonly XmlSerializer ahaSerializer = new(typeof(FritzBoxDeviceList));

    /// <summary><see cref="FritzBoxDevice.FritzBoxService"/> has a private getter: the devices only call it.</summary>
    private static readonly PropertyInfo fritzBoxServiceProperty = typeof(FritzBoxDevice).GetProperty(nameof(FritzBoxDevice.FritzBoxService), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

    private readonly ILoggerFactory loggers = LoggerFactory.Create(builder => builder.AddTestOutput());

    public void Dispose() => loggers.Dispose();

    /// <summary>
    /// Reads <c>jason_boxinfo.xml</c>, which the box serves to anyone, and nothing else: no login, so no password is
    /// needed, no wrong password can block the box, and nothing is written.
    /// </summary>
    [SystemFact]
    public async Task The_box_tells_its_FRITZ_OS_version_without_a_login_and_it_is_8_20_or_later()
    {
        // The password is never sent, because the version is read without a session.
        using var http = CreateClient(string.Empty);

        var version = await http.GetFritzOsVersionAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(version);
        output.WriteLine($"The box runs FRITZ!OS {version}");
        Assert.True(version >= restApiVersion, $"The box runs FRITZ!OS {version}, but the Smart Home REST API needs {restApiVersion} or later");
    }

    /// <summary>
    /// Logs in once with the configured user (<c>login_sid.lua</c>) and reads the smart home overview with the
    /// session, which is a read only request. Nothing is written. The box counts one more session, which it lets
    /// expire.
    /// </summary>
    [SystemFact]
    public async Task Logging_in_with_the_configured_user_works()
    {
        var token = TestContext.Current.CancellationToken;
        using var http = CreateClient();

        await http.LogInAsync(token);
        var overview = await new SmartHomeClient(http).GetOverviewAsync(token);

        Assert.NotEmpty(overview.Units);
    }

    /// <summary>
    /// Logs in with a password that is wrong on purpose. <b>This costs a few seconds of block time</b>: after a wrong
    /// password the box delays every login, also one with the right password, for a while, and the box writes the
    /// failed login into its event log (and may send its push mail about it, if it is set up to). A second client with
    /// the right password then logs in, and its wait for the block to end is part of this test, so that the tests
    /// which follow do not have to wait for it. The block grows with every further wrong password, which is why this
    /// is the only test that sends one, and only once.
    /// </summary>
    [SystemFact]
    public async Task A_wrong_password_is_refused_with_an_UnauthorizedAccessException_and_the_right_one_works_again_after_the_block()
    {
        var token = TestContext.Current.CancellationToken;

        // Made first, so that a missing password stops the test before anything wrong is sent to the box.
        using var right = CreateClient();

        using (var wrong = CreateClient($"wrong {Guid.NewGuid():N}"))
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => wrong.LogInAsync(token));
        }

        var stopwatch = Stopwatch.StartNew();
        await right.LogInAsync(token);
        output.WriteLine($"The login with the right password succeeded {stopwatch.Elapsed.TotalSeconds:N1} s after the refusal (includes the block time the box named)");
    }

    /// <summary>
    /// Reads the REST overview (<c>GET api/v0/smarthome/overview</c>) after a login and maps it with the library's
    /// mapper. Nothing is written. The box in Neufahrn has outlets and lamps, so a mapper that stops recognizing
    /// either would otherwise pass the checks of the outlets or lamps with nothing to check; that is why both must be
    /// there.
    /// </summary>
    [SystemFact]
    public async Task The_REST_overview_maps_to_devices_with_unique_AINs_and_ids_and_with_complete_outlets_and_lamps()
    {
        using var http = CreateClient();

        var overview = await new SmartHomeClient(http).GetOverviewAsync(TestContext.Current.CancellationToken);
        var devices = SmartHomeMapper.ToDeviceList(overview).Devices;

        Assert.NotEmpty(devices);

        // One device per unit, group units aside, as AHA's <group> elements always were.
        var units = overview.Units.Where(unit => !unit.IsGroupUnit).ToList();
        Assert.Equal(units.Count, devices.Count);

        Assert.All(units, unit => Assert.False(string.IsNullOrWhiteSpace(unit.Ain), $"The unit '{unit.Uid}' ({unit.Name}) has no AIN"));
        Assert.Empty(units.GroupBy(unit => unit.Ain, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => $"AIN '{group.Key}' belongs to the units {string.Join(", ", group.Select(unit => unit.Uid))}"));
        Assert.Empty(devices.GroupBy(device => device.Id).Where(group => group.Count() > 1).Select(group => $"Id {group.Key} belongs to the AINs {string.Join(", ", group.Select(device => device.Ain))}"));

        var outlets = devices.Where(device => device.Features.HasFlag(FritzBoxFeatures.AcOutlet)).ToList();
        var lamps = devices.Where(device => device.Features.HasFlag(FritzBoxFeatures.Light)).ToList();
        output.WriteLine($"{devices.Count} devices: {outlets.Count} outlets, {lamps.Count} lamps");

        Assert.NotEmpty(outlets);
        Assert.NotEmpty(lamps);

        Assert.Empty(outlets.Where(outlet => outlet.PowerMeter == null || outlet.Switch == null)
            .Select(outlet => $"The outlet '{outlet.Ain}' ({outlet.DisplayName}, present: {outlet.IsPresent}) has {(outlet.PowerMeter == null ? "no power meter" : "a power meter")} and {(outlet.Switch == null ? "no switch" : "a switch")}"));

        // A lamp is addressed by its unit, <device AIN>-1.
        Assert.Empty(lamps.Where(lamp => !lamp.Ain.EndsWith("-1", StringComparison.Ordinal)).Select(lamp => $"The lamp '{lamp.Ain}' ({lamp.DisplayName}) has an AIN that does not end in -1"));
    }

    /// <summary>
    /// Reads the REST overview and AHA's <c>switchcmd=getdevicelistinfos</c> after a login, the one right after the
    /// other, and compares what can be switched in each. Nothing is written. If a state differs, both are read once
    /// more before the test fails, because somebody may have switched in between.
    /// </summary>
    [SystemFact]
    public async Task REST_and_AHA_describe_the_same_switchable_units_with_the_same_names_and_states()
    {
        var token = TestContext.Current.CancellationToken;
        using var http = CreateClient();

        var both = await ReadBothAsync(http, token);

        if (Differences(both, "state", device => ((ISwitchable)device).IsTurnedOn).Count > 0)
        {
            both = await ReadBothAsync(http, token);
        }

        output.WriteLine($"{both.Rest.Count} switchable units over REST, {both.Aha.Count} over AHA");
        Assert.NotEmpty(both.Rest);
        Assert.Equal(both.Aha.Select(device => device.Ain).Order(StringComparer.Ordinal), both.Rest.Select(device => device.Ain).Order(StringComparer.Ordinal));
        Assert.Empty(Differences(both, "name", device => device.DisplayName));
        Assert.Empty(Differences(both, "state", device => ((ISwitchable)device).IsTurnedOn));
    }

    /// <summary>
    /// <see cref="FritzBoxSmartHomeService"/> from end to end: it reads the version (<c>jason_boxinfo.xml</c>), logs in
    /// and reads the REST overview once for its own list, and once more (with a client of its own) for what the list
    /// is held against. Nothing is written.
    /// </summary>
    /// <remarks>
    /// AHA's <c>fwversion</c> of the device list is the FRITZ!OS version as well, so the version alone does not tell
    /// the two interfaces apart. The ids do: REST units get a hash of their UID, AHA's devices small numbers the box
    /// hands out, and AHA lists a lamp twice where REST lists it once.
    /// </remarks>
    [SystemFact]
    public async Task The_service_uses_the_REST_API_on_this_box_and_gives_every_device_itself_as_its_service()
    {
        var token = TestContext.Current.CancellationToken;

        using var boxInfo = CreateClient(string.Empty);
        var version = await boxInfo.GetFritzOsVersionAsync(token);
        Assert.NotNull(version);

        using var http = CreateClient();
        var restDevices = SmartHomeMapper.ToDeviceList(await new SmartHomeClient(http).GetOverviewAsync(token)).Devices;

        using var service = CreateService();
        var list = await service.GetDevices(token);

        Assert.Equal(FormattableString.Invariant($"{version.Major}.{version.Minor:00}"), list.FirmwareVersionString);
        Assert.NotEmpty(list.Devices);

        // The same units under the same ids: had the service used AHA, the ids would be AHA's and the lamps there twice.
        Assert.Equal(restDevices.Select(device => (device.Ain, device.Id)).Order(), list.Devices.Select(device => (device.Ain, device.Id)).Order());
        Assert.All(list.Devices, device => Assert.Same(service, fritzBoxServiceProperty.GetValue(device)));
    }

    /// <summary>
    /// <b>Writes to the box, but changes nothing.</b> Sends one <c>PUT api/v0/smarthome/overview/triggers/{UID}</c>
    /// for the first routine of the overview, with the state that routine already has, reads the overview again after
    /// about 1.5 s (the box needs about a second to show a command) and checks that every routine is as it was. No unit,
    /// template or other routine is written. Skipped on a box that has no routine.
    /// </summary>
    [SystemFact]
    public async Task A_routine_written_with_the_state_it_already_has_stays_as_it_is()
    {
        var token = TestContext.Current.CancellationToken;
        using var http = CreateClient();
        var client = new SmartHomeClient(http);

        var before = await client.GetOverviewAsync(token);

        if (before.Triggers.Count == 0)
        {
            Assert.Skip("The box has no routine, so there is none to write to");
            return;
        }

        var trigger = before.Triggers[0];
        output.WriteLine($"Writing the routine '{trigger.Name}' ({trigger.Uid}) with the state it has: {(trigger.Enabled ? "enabled" : "disabled")}");

        await client.SetTriggerEnabledAsync(trigger.Uid, trigger.Enabled, token);
        await Task.Delay(TimeSpan.FromSeconds(1.5), token);
        var after = await client.GetOverviewAsync(token);

        Assert.Equal(DescribeTriggers(before.Triggers), DescribeTriggers(after.Triggers));
    }

    /// <summary>
    /// <b>Switches an outlet in the house, and only on request:</b> the environment variable
    /// <c>FRITZBOX_TEST_AIN</c> names it (with or without the space of its AIN). The test switches it to the opposite
    /// state through <see cref="FritzBoxSmartHomeService.SwitchDevice"/>, waits until the overview shows that (every
    /// 500 ms, at most 10 s), switches it back and waits again. Whatever happens in between, a <c>finally</c> puts the
    /// outlet in its original state. Name an outlet whose load can bear being switched off or on for some seconds (not a
    /// freezer): it stays in the opposite state for as long as the box needs to show it, which is a few seconds.
    /// Without the variable the test is skipped. A lamp, an outlet that is locked against switching through the API and
    /// one that is out of radio range are refused before anything is switched.
    /// </summary>
    [SystemFact]
    public async Task An_outlet_named_by_FRITZBOX_TEST_AIN_is_switched_to_the_opposite_state_and_back()
    {
        var ain = Environment.GetEnvironmentVariable(TestAinVariable);

        if (string.IsNullOrWhiteSpace(ain))
        {
            Assert.Skip($"Nothing is switched unless the environment variable {TestAinVariable} names the outlet to switch, for example 08761 0004275");
            return;
        }

        var token = TestContext.Current.CancellationToken;
        using var service = CreateService();
        var outlet = await ReadDeviceAsync(service, ain, token);

        if (!outlet.Features.HasFlag(FritzBoxFeatures.AcOutlet))
        {
            Assert.Fail($"{TestAinVariable} names '{outlet.Ain}' ({outlet.DisplayName}), which is not an outlet");
        }

        if (!outlet.IsPresent)
        {
            Assert.Fail($"The outlet '{outlet.Ain}' ({outlet.DisplayName}) is not connected to the box right now");
        }

        if (outlet.Switch is { IsUiLocked: true })
        {
            Assert.Fail($"The outlet '{outlet.Ain}' ({outlet.DisplayName}) is locked against switching through the web interface and the API");
        }

        var state = ((ISwitchable)outlet).IsTurnedOn;

        if (state == null)
        {
            Assert.Fail($"The box does not say whether the outlet '{outlet.Ain}' ({outlet.DisplayName}) is on");
        }

        var original = state.Value;
        output.WriteLine($"The outlet '{outlet.Ain}' ({outlet.DisplayName}) is {(original ? "on" : "off")}");

        try
        {
            await service.SwitchDevice(outlet.Ain, !original, token);
            await WaitForStateAsync(service, outlet.Ain, !original, token);

            await service.SwitchDevice(outlet.Ain, original, token);
            await WaitForStateAsync(service, outlet.Ain, original, token);
        }
        finally
        {
            await RestoreAsync(service, outlet.Ain, original);
        }
    }

    #region The box

    private static string GetPassword()
    {
        var password = Environment.GetEnvironmentVariable(PasswordVariable);

        if (string.IsNullOrEmpty(password))
        {
            Assert.Fail($"The environment variable {PasswordVariable} holds no password. Set it to the password of the user of the FRITZ!Box at {BoxUrl} (and {UserVariable} to the user, if it is not the one that logged in last) in the environment of the process that runs the tests.");
        }

        return password;
    }

    private static string? GetUserName() => Environment.GetEnvironmentVariable(UserVariable);

    /// <summary>A client for the box. The password is the one in the environment unless <paramref name="password"/> says which.</summary>
    private FritzBoxHttpClient CreateClient(string? password = null) => new(new FritzBoxHttpClientOptions
    {
        BaseUri = new Uri(BoxUrl + "/"),
        UserName = GetUserName(),
        Password = password ?? GetPassword(),
    }, loggers.CreateLogger<FritzBoxHttpClient>());

    private FritzBoxSmartHomeService CreateService() => new(loggers)
    {
        Connection = new WebConnection { BaseUrl = BoxUrl, UserName = GetUserName() ?? string.Empty, Password = GetPassword() },
    };

    #endregion

    #region REST against AHA

    private sealed record BothInterfaces(IReadOnlyList<FritzBoxDevice> Rest, IReadOnlyList<FritzBoxDevice> Aha);

    /// <summary>The switchable devices of the REST overview (as the library maps it) and of AHA, read one right after the other.</summary>
    private static async Task<BothInterfaces> ReadBothAsync(FritzBoxHttpClient http, CancellationToken token)
    {
        var overview = await new SmartHomeClient(http).GetOverviewAsync(token);
        var rest = SmartHomeMapper.ToDeviceList(overview).Devices.Where(device => device.CanSwitch).ToList();

        using var reader = new StringReader(await http.GetAhaAsync("switchcmd=getdevicelistinfos", token));
        var aha = (ahaSerializer.Deserialize(reader) as FritzBoxDeviceList ?? throw new InvalidDataException("getdevicelistinfos answered with an empty device list")).Devices.Where(device => device.CanSwitch).ToList();

        return new BothInterfaces(rest, aha);
    }

    /// <summary>What <paramref name="select"/> says differently about the units both interfaces list, as one line for each.</summary>
    private static List<string> Differences(BothInterfaces both, string what, Func<FritzBoxDevice, object?> select)
    {
        var aha = both.Aha.ToDictionary(device => device.Ain, StringComparer.Ordinal);

        return
        [
            .. both.Rest
                .Where(rest => aha.TryGetValue(rest.Ain, out var other) && !Equals(select(rest), select(other)))
                .Select(rest => $"The {what} of '{rest.Ain}' is {Describe(select(rest))} over REST and {Describe(select(aha[rest.Ain]))} over AHA"),
        ];
    }

    private static string Describe(object? value) => value switch
    {
        null => "unknown",
        true => "on",
        false => "off",
        _ => $"'{value}'",
    };

    /// <summary>The routines as one line each, sorted by UID, so that two overviews can be compared.</summary>
    private static List<string> DescribeTriggers(IEnumerable<SmartHomeTrigger> triggers) =>
        [.. triggers.Select(trigger => $"{trigger.Uid} | {trigger.Name} | {(trigger.Enabled ? "enabled" : "disabled")}").Order(StringComparer.Ordinal)];

    #endregion

    #region Switching

    private static string Compact(string ain) => ain.Replace(" ", string.Empty, StringComparison.Ordinal);

    /// <summary>The device with <paramref name="ain"/> (with or without spaces) in what the service reads now.</summary>
    private static async Task<FritzBoxDevice> ReadDeviceAsync(FritzBoxSmartHomeService service, string ain, CancellationToken token)
    {
        var devices = (await service.GetDevices(token)).Devices;
        var device = devices.SingleOrDefault(candidate => Compact(candidate.Ain) == Compact(ain));

        if (device == null)
        {
            var outlets = devices.Where(candidate => candidate.Features.HasFlag(FritzBoxFeatures.AcOutlet)).Select(candidate => $"{candidate.Ain} ({candidate.DisplayName})");
            Assert.Fail($"The box has no unit with the AIN '{ain}'. It has these outlets: {string.Join(", ", outlets)}");
        }

        return device;
    }

    /// <summary>Reads the box every 500 ms until the outlet shows <paramref name="expected"/>, and fails after 10 s.</summary>
    private async Task WaitForStateAsync(FritzBoxSmartHomeService service, string ain, bool expected, CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            var state = ((ISwitchable)await ReadDeviceAsync(service, ain, token)).IsTurnedOn;

            if (state == expected)
            {
                output.WriteLine($"The overview shows '{ain}' {(expected ? "on" : "off")} after {stopwatch.ElapsedMilliseconds} ms");
                return;
            }

            if (stopwatch.Elapsed >= pollTimeout)
            {
                Assert.Fail($"The overview still shows '{ain}' as {Describe(state)} {stopwatch.Elapsed.TotalSeconds:N1} s after it was switched {(expected ? "on" : "off")}");
            }

            await Task.Delay(pollInterval, token);
        }
    }

    /// <summary>
    /// Puts the outlet back in <paramref name="original"/> if it is not. It runs in a <c>finally</c>, so it must not
    /// replace the exception that got the test there: whatever goes wrong here is written to the test output, where the
    /// developer is told to switch the outlet by hand. It has a token of its own, because the test's may be the reason
    /// for being here.
    /// </summary>
    private async Task RestoreAsync(FritzBoxSmartHomeService service, string ain, bool original)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        try
        {
            if (((ISwitchable)await ReadDeviceAsync(service, ain, timeout.Token)).IsTurnedOn != original)
            {
                await service.SwitchDevice(ain, original, timeout.Token);
                await WaitForStateAsync(service, ain, original, timeout.Token);
            }
        }
        catch (Exception exception)
        {
            output.WriteLine($"THE OUTLET '{ain}' COULD NOT BE PUT BACK IN ITS ORIGINAL STATE ({(original ? "on" : "off")}). Switch it by hand. {exception}");
        }
    }

    #endregion
}
