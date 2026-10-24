using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using De.Hochstaetter.FritzBox.Http;
using De.Hochstaetter.FritzBox.SmartHome;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Settings;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// <see cref="FritzBoxSmartHomeService"/> against a fake box: the choice between the Smart Home REST API (FRITZ!OS 8.20
/// and later) and AHA from <c>jason_boxinfo.xml</c>, what each of them is asked for and in which words, the UID lookup
/// of the REST API, and the handling of a changed connection. The fake box answers with the fixtures of
/// <see cref="FritzBoxSmartHomeFixtures"/>; the login is the fake box's own and is tested in
/// <see cref="FritzBoxHttpClientTests"/>.
/// </summary>
public sealed class FritzBoxSmartHomeServiceTests
{
    private const string Os825 = "254.08.25";
    private const string Os757 = "154.07.57";
    private const string BaseUrl = "http://192.168.44.11";
    private const string OverviewPath = "/api/v0/smarthome/overview";
    private const string UnitsPath = "/api/v0/smarthome/overview/units/";
    private const string AhaPath = "/webservices/homeautoswitch.lua";
    private const string BoxInfoPath = "/jason_boxinfo.xml";
    private const string LoginPath = "/login_sid.lua?version=2";

    /// <summary>A lamp: its unit has the AIN "13077 0000003-1" and the same UID.</summary>
    private const string LampAin = "13077 0000003-1";

    private const string LampUnitPath = UnitsPath + "13077%200000003-1";

    private static readonly string[] restAins = ["08761 0000001", "11630 0000002", "13077 0000003-1", "13077 0000004-1", "08761 0000005"];
    private static readonly string[] ahaAins = ["08761 0000001", "11630 0000002", "13077 0000003", "13077 0000003-1", "13077 0000004", "13077 0000004-1", "08761 0000005"];

    /// <summary><see cref="FritzBoxDevice.FritzBoxService"/> has a private getter: the devices only call it.</summary>
    private static readonly PropertyInfo fritzBoxServiceProperty = typeof(FritzBoxDevice).GetProperty(nameof(FritzBoxDevice.FritzBoxService), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

    #region REST API (FRITZ!OS 8.20 and later)

    [Fact]
    public async Task On_FRITZ_OS_8_25_GetDevices_reads_the_REST_overview_and_never_asks_AHA()
    {
        var box = new FakeBox(Os825);
        using var service = CreateService(box);

        var list = await service.GetDevices(TestContext.Current.CancellationToken);

        Assert.Equal([$"GET {BoxInfoPath}", $"GET {OverviewPath}"], box.Requests.Select(c => c.Line));
        Assert.DoesNotContain(box.Calls, c => c.Path == AhaPath);
        Assert.Equal(restAins.Order(), list.Devices.Select(d => d.Ain).Order());
        Assert.All(list.Devices, device => Assert.Same(service, ServiceOf(device)));
        Assert.Equal("8.25", list.FirmwareVersionString);
    }

    [Theory]
    [InlineData("254.08.25", "8.25")]
    [InlineData("254.08.20", "8.20")]
    [InlineData("254.10.05", "10.05")]
    public async Task The_firmware_of_a_REST_list_is_the_FRITZ_OS_version_of_the_box_with_a_two_digit_minor(string boxVersion, string expected)
    {
        var box = new FakeBox(boxVersion);
        using var service = CreateService(box);

        var list = await service.GetDevices(TestContext.Current.CancellationToken);

        Assert.Equal(expected, list.FirmwareVersionString);
    }

    [Theory]
    [InlineData("08761 0000001")]
    [InlineData("11630 0000002")]
    [InlineData("13077 0000003-1")]
    [InlineData("13077 0000004-1")]
    [InlineData("08761 0000005")]
    public async Task A_device_of_the_REST_list_switches_its_unit_through_the_service_without_another_overview(string ain)
    {
        var box = new FakeBox(Os825);
        using var service = CreateService(box);
        var list = await service.GetDevices(TestContext.Current.CancellationToken);

        await list.Devices.Single(d => d.Ain == ain).TurnOnOff(true);

        AssertPut(box, UnitsPath + ain.Replace(" ", "%20", StringComparison.Ordinal), SwitchBody(true));
        Assert.Single(box.OverviewRequests);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SwitchDevice_puts_the_onOffInterface_to_the_unit_with_the_space_of_its_UID_escaped(bool turnOn)
    {
        var box = new FakeBox(Os825);
        using var service = CreateService(box);

        await service.SwitchDevice(LampAin, turnOn, TestContext.Current.CancellationToken);

        AssertRestCommand(box, LampUnitPath, SwitchBody(turnOn));
        Assert.Equal($"AVM-SID {FakeBox.Sid(1)}", Assert.Single(box.Puts).Authorization);
    }

    [Theory]
    [InlineData(0.5, 50)]
    [InlineData(0, 1)]
    [InlineData(7, 100)]
    [InlineData(1, 100)]
    [InlineData(-2, 1)]
    [InlineData(0.125, 13)]
    [InlineData(0.994, 99)]
    public async Task SetLevel_puts_the_percentage_between_1_and_100(double level, int percent)
    {
        var box = new FakeBox(Os825);
        using var service = CreateService(box);

        await service.SetLevel(LampAin, level, TestContext.Current.CancellationToken);

        AssertRestCommand(box, LampUnitPath, Interfaces("levelControlInterface", new JsonObject { ["level"] = percent }));
    }

    [Theory]
    [InlineData(3000.4, 3000)]
    [InlineData(2700, 2700)]
    [InlineData(3000.5, 3001)]
    [InlineData(6500, 6500)]
    public async Task SetColorTemperature_puts_the_rounded_kelvin_to_the_colorControlInterface(double kelvin, int expected)
    {
        var box = new FakeBox(Os825);
        using var service = CreateService(box);

        await service.SetColorTemperature(LampAin, kelvin, TestContext.Current.CancellationToken);

        AssertRestCommand(box, LampUnitPath, Interfaces("colorControlInterface", new JsonObject { ["colorTemperature"] = expected }));
    }

    [Theory]
    [InlineData(400, 2, 359, 255)]
    [InlineData(120, 0.5, 120, 128)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(359.4, 1, 359, 255)]
    [InlineData(359.6, 1, 359, 255)]
    [InlineData(-5, -1, 0, 0)]
    public async Task SetColor_puts_hue_up_to_359_and_saturation_up_to_255_as_the_hsColor(double hue, double saturation, int expectedHue, int expectedSaturation)
    {
        var box = new FakeBox(Os825);
        using var service = CreateService(box);

        await service.SetColor(LampAin, hue, saturation, TestContext.Current.CancellationToken);

        AssertRestCommand(box, LampUnitPath, Interfaces("colorControlInterface", new JsonObject { ["hsColor"] = new JsonObject { ["hue"] = expectedHue, ["saturation"] = expectedSaturation } }));
    }

    #endregion

    #region REST API: the UID of a unit

    [Theory]
    [InlineData("13077 0000003-1", "13077%200000003-1")]
    [InlineData("130770000003-1", "13077%200000003-1")]
    [InlineData("08761 0000001", "08761%200000001")]
    [InlineData("087610000001", "08761%200000001")]
    public async Task An_AIN_with_or_without_spaces_reaches_the_UID_of_the_overview_at_the_cost_of_one_overview(string ain, string escapedUid)
    {
        var box = new FakeBox(Os825);
        using var service = CreateService(box);

        await service.SwitchDevice(ain, true, TestContext.Current.CancellationToken);

        AssertRestCommand(box, UnitsPath + escapedUid, SwitchBody(true));
    }

    [Theory]
    [InlineData("13077 0000003-1")]
    [InlineData("130770000003-1")]
    public async Task After_GetDevices_the_UID_is_known_and_a_command_costs_no_overview(string ain)
    {
        var box = new FakeBox(Os825);
        using var service = CreateService(box);
        await service.GetDevices(TestContext.Current.CancellationToken);

        await service.SwitchDevice(ain, true, TestContext.Current.CancellationToken);

        Assert.Single(box.OverviewRequests);
        Assert.Equal(LampUnitPath, Assert.Single(box.Puts).PathAndQuery);
    }

    [Fact]
    public async Task The_overview_is_read_for_the_first_command_only()
    {
        var box = new FakeBox(Os825);
        using var service = CreateService(box);
        var token = TestContext.Current.CancellationToken;

        await service.SwitchDevice(LampAin, true, token);
        await service.SetLevel("130770000003-1", 0.5, token);
        await service.SetColor("13077 0000004-1", 90, 1, token);

        Assert.Single(box.OverviewRequests);
        Assert.Equal([LampUnitPath, LampUnitPath, UnitsPath + "13077%200000004-1"], box.Puts.Select(c => c.PathAndQuery));
    }

    [Theory]
    [InlineData("99999 0000009", "99999%200000009")]
    [InlineData("999990000009", "999990000009")]
    public async Task A_unit_the_overview_does_not_know_is_addressed_by_its_AIN_as_it_is(string ain, string escapedUid)
    {
        var box = new FakeBox(Os825);
        using var service = CreateService(box);

        await service.SwitchDevice(ain, true, TestContext.Current.CancellationToken);

        AssertRestCommand(box, UnitsPath + escapedUid, SwitchBody(true));
    }

    #endregion

    #region AHA (FRITZ!OS before 8.20)

    [Fact]
    public async Task On_FRITZ_OS_7_57_GetDevices_asks_AHA_for_the_device_list_info_and_never_the_REST_API()
    {
        var box = new FakeBox(Os757);
        using var service = CreateService(box);

        var list = await service.GetDevices(TestContext.Current.CancellationToken);

        Assert.Equal([$"GET {BoxInfoPath}", $"GET {AhaPath}?switchcmd=getdevicelistinfos&sid={FakeBox.Sid(1)}"], box.Requests.Select(c => c.Line));
        Assert.DoesNotContain(box.Calls, c => c.IsRest);
        Assert.Equal(ahaAins.Order(), list.Devices.Select(d => d.Ain).Order());
        Assert.All(list.Devices, device => Assert.Same(service, ServiceOf(device)));
    }

    [Theory]
    [InlineData("08761 0000001")]
    [InlineData("11630 0000002")]
    [InlineData("13077 0000003")]
    [InlineData("13077 0000003-1")]
    [InlineData("13077 0000004")]
    [InlineData("13077 0000004-1")]
    [InlineData("08761 0000005")]
    public async Task A_device_of_the_AHA_list_switches_through_the_service_by_its_AIN_without_spaces(string ain)
    {
        var box = new FakeBox(Os757);
        using var service = CreateService(box);
        var list = await service.GetDevices(TestContext.Current.CancellationToken);

        await list.Devices.Single(d => d.Ain == ain).TurnOnOff(false);

        Assert.Equal($"{AhaPath}?ain={ain.Replace(" ", string.Empty, StringComparison.Ordinal)}&switchcmd=setswitchoff&sid={FakeBox.Sid(1)}", box.Requests[^1].PathAndQuery);
        Assert.DoesNotContain(box.Calls, c => c.IsRest);
    }

    [Theory]
    [InlineData(false, "setswitchoff")]
    [InlineData(true, "setswitchon")]
    public async Task Over_AHA_SwitchDevice_sends_setswitchon_or_setswitchoff_for_the_AIN_without_spaces(bool turnOn, string command)
    {
        var box = new FakeBox(Os757);
        using var service = CreateService(box);

        await service.SwitchDevice(LampAin, turnOn, TestContext.Current.CancellationToken);

        AssertAhaCommand(box, $"ain=130770000003-1&switchcmd={command}");
    }

    [Theory]
    [InlineData("13077 0000003-1", "130770000003-1")]
    [InlineData("130770000003-1", "130770000003-1")]
    [InlineData("08761 0000001", "087610000001")]
    public async Task Over_AHA_an_AIN_is_sent_without_spaces_however_it_is_written(string ain, string expected)
    {
        var box = new FakeBox(Os757);
        using var service = CreateService(box);

        await service.SwitchDevice(ain, true, TestContext.Current.CancellationToken);

        AssertAhaCommand(box, $"ain={expected}&switchcmd=setswitchon");
    }

    [Theory]
    [InlineData(0.5, 128)]
    [InlineData(0, 2)]
    [InlineData(1, 255)]
    [InlineData(7, 255)]
    [InlineData(-1, 2)]
    [InlineData(0.01, 3)]
    public async Task Over_AHA_SetLevel_sends_the_level_between_2_and_255(double level, int absolute)
    {
        var box = new FakeBox(Os757);
        using var service = CreateService(box);

        await service.SetLevel(LampAin, level, TestContext.Current.CancellationToken);

        AssertAhaCommand(box, $"ain=130770000003-1&switchcmd=setlevel&level={absolute}");
    }

    [Theory]
    [InlineData(2700, 2700)]
    [InlineData(3000.4, 3000)]
    public async Task Over_AHA_SetColorTemperature_sends_the_rounded_kelvin_with_duration_0(double kelvin, int expected)
    {
        var box = new FakeBox(Os757);
        using var service = CreateService(box);

        await service.SetColorTemperature(LampAin, kelvin, TestContext.Current.CancellationToken);

        AssertAhaCommand(box, $"ain=130770000003-1&switchcmd=setcolortemperature&temperature={expected}&duration=0");
    }

    [Theory]
    [InlineData(120, 0.5, 120, 128)]
    [InlineData(400, 2, 359, 255)]
    public async Task Over_AHA_SetColor_sends_an_unmapped_color_with_duration_0(double hue, double saturation, int expectedHue, int expectedSaturation)
    {
        var box = new FakeBox(Os757);
        using var service = CreateService(box);

        await service.SetColor(LampAin, hue, saturation, TestContext.Current.CancellationToken);

        AssertAhaCommand(box, $"ain=130770000003-1&switchcmd=setunmappedcolor&hue={expectedHue}&saturation={expectedSaturation}&duration=0");
    }

    #endregion

    #region REST or AHA: the version of the box

    [Theory]
    [InlineData("254.08.25", true)]
    [InlineData("254.08.20", true)]
    [InlineData("254.10.05", true)]
    [InlineData("254.08.19", false)]
    [InlineData("254.08.05", false)]
    [InlineData("154.07.57", false)]
    public async Task The_REST_API_is_used_from_FRITZ_OS_8_20_on_and_AHA_before(string boxVersion, bool usesRestApi)
    {
        var box = new FakeBox(boxVersion);
        using var service = CreateService(box);

        await service.GetDevices(TestContext.Current.CancellationToken);

        Assert.Equal(usesRestApi, box.Calls.Any(c => c.IsRest));
        Assert.Equal(!usesRestApi, box.Calls.Any(c => c.Path == AhaPath));
    }

    [Fact]
    public async Task Without_jason_boxinfo_AHA_is_used()
    {
        var box = new FakeBox(null);
        using var service = CreateService(box);

        var list = await service.GetDevices(TestContext.Current.CancellationToken);

        Assert.Equal([$"GET {BoxInfoPath}", $"GET {AhaPath}?switchcmd=getdevicelistinfos&sid={FakeBox.Sid(1)}"], box.Requests.Select(c => c.Line));
        Assert.Equal(ahaAins.Order(), list.Devices.Select(d => d.Ain).Order());
        Assert.All(list.Devices, device => Assert.Same(service, ServiceOf(device)));
    }

    [Fact]
    public async Task Without_jason_boxinfo_a_command_goes_over_AHA_too()
    {
        var box = new FakeBox(null);
        using var service = CreateService(box);

        await service.SwitchDevice(LampAin, true, TestContext.Current.CancellationToken);

        AssertAhaCommand(box, "ain=130770000003-1&switchcmd=setswitchon");
    }

    [Theory]
    [InlineData("<unrelated/>")]
    [InlineData("""<j:BoxInfo xmlns:j="http://jason.avm.de/updatecheck/"><j:Name>FRITZ!Box</j:Name></j:BoxInfo>""")]
    [InlineData("""<j:BoxInfo xmlns:j="http://jason.avm.de/updatecheck/"><j:Version>not a version</j:Version></j:BoxInfo>""")]
    [InlineData("""<j:BoxInfo xmlns:j="http://jason.avm.de/updatecheck/"><j:Version>8.25</j:Version></j:BoxInfo>""")]
    public async Task A_jason_boxinfo_without_a_readable_version_means_AHA(string body)
    {
        var box = new FakeBox { BoxInfo = (HttpStatusCode.OK, body) };
        using var service = CreateService(box);

        await service.GetDevices(TestContext.Current.CancellationToken);

        Assert.DoesNotContain(box.Calls, c => c.IsRest);
        Assert.Contains(box.Calls, c => c.Path == AhaPath);
    }

    /// <remarks>
    /// Production bug, reported and not fixed: <c>FritzBoxHttpClient.GetFritzOsVersionAsync</c> promises
    /// <see langword="null"/> when the box's answer "says nothing readable", but <c>XDocument.Parse</c> (line 65) throws an
    /// <see cref="System.Xml.XmlException"/> for a body that is not well-formed XML (an empty one, an HTML page). The
    /// exception ends GetDevices and every command, on every call, instead of falling back to AHA.
    /// </remarks>
    [Theory(Skip = "Bug in FritzBoxHttpClient.GetFritzOsVersionAsync (line 65): a jason_boxinfo.xml that is not well-formed XML throws an XmlException instead of giving null, which would mean AHA")]
    [InlineData("")]
    [InlineData("<html><body>Please log in<br></body></html>")]
    public async Task A_jason_boxinfo_that_is_not_XML_means_AHA(string body)
    {
        var box = new FakeBox { BoxInfo = (HttpStatusCode.OK, body) };
        using var service = CreateService(box);

        await service.GetDevices(TestContext.Current.CancellationToken);

        Assert.DoesNotContain(box.Calls, c => c.IsRest);
        Assert.Contains(box.Calls, c => c.Path == AhaPath);
    }

    [Theory]
    [InlineData(Os825)]
    [InlineData(Os757)]
    [InlineData(null)]
    public async Task The_version_is_read_once_per_connection_whichever_way_it_came_out(string? boxVersion)
    {
        var box = new FakeBox(boxVersion);
        using var service = CreateService(box);

        await service.GetDevices(TestContext.Current.CancellationToken);
        await service.GetDevices(TestContext.Current.CancellationToken);

        Assert.Single(box.BoxInfoRequests);
    }

    [Fact]
    public async Task Calls_of_every_kind_share_one_version_check_one_session_and_one_overview()
    {
        var box = new FakeBox(Os825);
        using var service = CreateService(box);
        var token = TestContext.Current.CancellationToken;

        await service.GetDevices(token);
        await service.SwitchDevice(LampAin, true, token);
        await service.SetLevel(LampAin, 0.5, token);
        await service.SetColorTemperature(LampAin, 3000, token);
        await service.SetColor(LampAin, 120, 0.5, token);

        Assert.Single(box.BoxInfoRequests);
        Assert.Single(box.LoginPosts);
        Assert.Single(box.OverviewRequests);
        Assert.Equal(4, box.Puts.Count);
    }

    #endregion

    #region The connection

    [Fact]
    public async Task A_new_connection_with_another_password_gets_a_new_client_and_the_version_is_read_again()
    {
        var box = new FakeBox(Os825);
        var created = new List<FritzBoxHttpClientOptions>();
        using var service = CreateService(box, created);
        var token = TestContext.Current.CancellationToken;

        await service.GetDevices(token);
        service.Connection = CreateConnection(password: "another");
        await service.GetDevices(token);

        Assert.Equal(["secret", "another"], created.Select(o => o.Password));
        Assert.Equal(2, box.BoxInfoRequests.Count);
    }

    [Theory]
    [InlineData(nameof(WebConnection.BaseUrl))]
    [InlineData(nameof(WebConnection.UserName))]
    [InlineData(nameof(WebConnection.Password))]
    public async Task Changing_the_address_the_user_or_the_password_of_the_connection_in_place_makes_the_next_call_create_a_new_client(string changed)
    {
        var box = new FakeBox(Os825);
        var created = new List<FritzBoxHttpClientOptions>();
        var connection = CreateConnection();
        using var service = CreateService(box, created, connection);
        var token = TestContext.Current.CancellationToken;

        await service.GetDevices(token);

        switch (changed)
        {
            case nameof(WebConnection.BaseUrl):
                connection.BaseUrl = "http://192.168.44.12";
                break;
            case nameof(WebConnection.UserName):
                connection.UserName = "another";
                break;
            case nameof(WebConnection.Password):
                connection.Password = "another";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(changed), changed, null);
        }

        await service.GetDevices(token);

        Assert.Equal(2, created.Count);
        Assert.Equal(2, box.BoxInfoRequests.Count);
        Assert.Equal(new Uri(connection.BaseUrl + "/"), created[1].BaseUri);
        Assert.Equal(connection.UserName, created[1].UserName);
        Assert.Equal(connection.Password, created[1].Password);
    }

    [Fact]
    public async Task The_same_connection_keeps_its_client_and_its_version()
    {
        var box = new FakeBox(Os825);
        var created = new List<FritzBoxHttpClientOptions>();
        using var service = CreateService(box, created);
        var token = TestContext.Current.CancellationToken;

        await service.GetDevices(token);
        await service.SwitchDevice(LampAin, true, token);
        await service.GetDevices(token);

        Assert.Single(created);
        Assert.Single(box.BoxInfoRequests);
    }

    [Fact]
    public async Task Another_connection_object_with_the_same_address_user_and_password_keeps_the_client()
    {
        var box = new FakeBox(Os825);
        var created = new List<FritzBoxHttpClientOptions>();
        using var service = CreateService(box, created);
        var token = TestContext.Current.CancellationToken;

        await service.GetDevices(token);
        service.Connection = CreateConnection();
        await service.GetDevices(token);

        Assert.Single(created);
        Assert.Single(box.BoxInfoRequests);
    }

    [Fact]
    public async Task A_new_connection_decides_between_REST_and_AHA_again()
    {
        var box = new FakeBox(Os825);
        using var service = CreateService(box);
        var token = TestContext.Current.CancellationToken;

        await service.GetDevices(token);
        box.BoxInfo = FakeBox.BoxInfoOf(Os757);
        service.Connection = CreateConnection(password: "another");
        var list = await service.GetDevices(token);

        Assert.Equal([BoxInfoPath, OverviewPath, BoxInfoPath, AhaPath], box.Requests.Select(c => c.Path));
        Assert.Equal(ahaAins.Order(), list.Devices.Select(d => d.Ain).Order());
    }

    [Fact]
    public async Task The_UIDs_that_were_learned_belong_to_the_connection()
    {
        var box = new FakeBox(Os825);
        using var service = CreateService(box);
        var token = TestContext.Current.CancellationToken;

        await service.SwitchDevice(LampAin, true, token);
        await service.SwitchDevice(LampAin, false, token);
        service.Connection = CreateConnection(password: "another");
        await service.SwitchDevice(LampAin, true, token);

        Assert.Equal(2, box.OverviewRequests.Count);
        Assert.Equal(3, box.Puts.Count);
    }

    [Theory]
    [InlineData("http://192.168.44.11", "http://192.168.44.11/")]
    [InlineData("http://192.168.44.11/", "http://192.168.44.11/")]
    [InlineData("http://192.168.44.11//", "http://192.168.44.11/")]
    [InlineData("https://192.168.44.11:49443", "https://192.168.44.11:49443/")]
    public async Task The_client_is_made_for_the_address_user_and_password_of_the_connection_and_accepts_the_self_signed_certificate(string baseUrl, string expectedBaseUri)
    {
        var box = new FakeBox(Os825);
        var created = new List<FritzBoxHttpClientOptions>();
        using var service = CreateService(box, created, CreateConnection(baseUrl));

        await service.GetDevices(TestContext.Current.CancellationToken);

        var options = Assert.Single(created);
        Assert.Equal(new Uri(expectedBaseUri), options.BaseUri);
        Assert.Equal("user", options.UserName);
        Assert.Equal("secret", options.Password);
        Assert.NotNull(options.AcceptUntrustedCertificate);
        Assert.True(options.AcceptUntrustedCertificate(null!), "The box's own certificate is self-signed and has always been accepted (the callback does not look at it)");
    }

    [Fact]
    public async Task The_client_of_a_replaced_connection_is_disposed_and_so_is_the_current_one_with_the_service_which_may_be_disposed_twice()
    {
        var box = new FakeBox(Os825);
        using var service = CreateService(box);
        var token = TestContext.Current.CancellationToken;

        await service.GetDevices(token);
        Assert.Equal(0, box.Disposals);

        service.Connection = CreateConnection(password: "another");
        await service.GetDevices(token);
        Assert.Equal(1, box.Disposals);

        service.Dispose();
        Assert.Equal(2, box.Disposals);

        service.Dispose();
        Assert.Equal(2, box.Disposals);
    }

    #endregion

    #region No connection

    [Theory]
    [InlineData(nameof(IFritzBoxService.FritzBoxLogin))]
    [InlineData(nameof(IFritzBoxService.GetDevices))]
    [InlineData(nameof(IFritzBoxService.SwitchDevice))]
    [InlineData(nameof(IFritzBoxService.SetLevel))]
    [InlineData(nameof(IFritzBoxService.SetColorTemperature))]
    [InlineData(nameof(IFritzBoxService.SetColor))]
    public async Task Without_a_connection_every_method_throws_an_InvalidOperationException_and_sends_nothing(string operation)
    {
        var box = new FakeBox(Os825);
        var created = new List<FritzBoxHttpClientOptions>();
        using var service = CreateService(box, created);
        service.Connection = null;

        await Assert.ThrowsAsync<InvalidOperationException>(() => Invoke(service, operation, TestContext.Current.CancellationToken));

        Assert.Empty(created);
        Assert.Empty(box.Calls);
    }

    [Theory]
    [InlineData(nameof(IFritzBoxService.GetDevices))]
    [InlineData(nameof(IFritzBoxService.SwitchDevice))]
    public async Task A_connection_that_is_taken_away_again_throws_an_InvalidOperationException(string operation)
    {
        var box = new FakeBox(Os825);
        using var service = CreateService(box);
        var token = TestContext.Current.CancellationToken;
        await service.GetDevices(token);

        service.Connection = null;

        await Assert.ThrowsAsync<InvalidOperationException>(() => Invoke(service, operation, token));
    }

    #endregion

    #region FritzBoxLogin

    [Fact]
    public async Task FritzBoxLogin_logs_in_even_when_a_session_exists()
    {
        var box = new FakeBox(Os825);
        using var service = CreateService(box);
        var token = TestContext.Current.CancellationToken;

        await service.GetDevices(token);
        Assert.Single(box.LoginPosts);

        await service.FritzBoxLogin(token);

        Assert.Equal(2, box.LoginGets.Count);
        Assert.Equal(2, box.LoginPosts.Count);
    }

    [Fact]
    public async Task The_calls_after_FritzBoxLogin_carry_the_new_session()
    {
        var box = new FakeBox(Os825);
        using var service = CreateService(box);
        var token = TestContext.Current.CancellationToken;

        await service.GetDevices(token);
        await service.FritzBoxLogin(token);
        await service.SwitchDevice(LampAin, true, token);

        Assert.Equal($"AVM-SID {FakeBox.Sid(1)}", Assert.Single(box.OverviewRequests).Authorization);
        Assert.Equal($"AVM-SID {FakeBox.Sid(2)}", Assert.Single(box.Puts).Authorization);
    }

    [Fact]
    public async Task FritzBoxLogin_alone_logs_in_once_without_reading_the_version_or_anything_else()
    {
        var box = new FakeBox(Os825);
        using var service = CreateService(box);

        await service.FritzBoxLogin(TestContext.Current.CancellationToken);

        Assert.Equal([$"GET {LoginPath}", $"POST {LoginPath}"], box.Calls.Select(c => c.Line));
    }

    [Fact]
    public async Task FritzBoxLogin_throws_an_UnauthorizedAccessException_when_the_box_refuses_the_login()
    {
        var box = new FakeBox(Os825) { RefuseLogin = true };
        using var service = CreateService(box);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.FritzBoxLogin(TestContext.Current.CancellationToken).AsTask());
    }

    #endregion

    #region Cancellation

    [Theory]
    [InlineData(nameof(IFritzBoxService.GetDevices), Os825)]
    [InlineData(nameof(IFritzBoxService.SwitchDevice), Os825)]
    [InlineData(nameof(IFritzBoxService.GetDevices), Os757)]
    [InlineData(nameof(IFritzBoxService.SwitchDevice), Os757)]
    public async Task Cancelling_the_token_cancels_the_request_that_waits_for_the_box(string operation, string boxVersion)
    {
        var arrived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var box = new FakeBox(boxVersion)
        {
            // The box never answers a request of the API, so the call can only end when the token reaches the HTTP client.
            Before = (call, token) =>
            {
                if (!call.IsApi)
                {
                    return Task.CompletedTask;
                }

                arrived.TrySetResult();
                return Task.Delay(TimeSpan.FromSeconds(30), token);
            },
        };

        using var service = CreateService(box);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        var pending = Invoke(service, operation, cancellation.Token);
        await arrived.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    }

    #endregion

    private static WebConnection CreateConnection(string baseUrl = BaseUrl, string password = "secret") => new() { BaseUrl = baseUrl, UserName = "user", Password = password };

    /// <summary>A service for the fake box, with a connection unless <paramref name="connection"/> says which. Every client it makes is recorded by its options.</summary>
    private static FritzBoxSmartHomeService CreateService(FakeBox box, List<FritzBoxHttpClientOptions>? created = null, WebConnection? connection = null) => new(NullLoggerFactory.Instance, options =>
    {
        created?.Add(options);
        return new FritzBoxHttpClient(options, null, box);
    })
    {
        Connection = connection ?? CreateConnection(),
    };

    private static object? ServiceOf(FritzBoxDevice device) => fritzBoxServiceProperty.GetValue(device);

    /// <summary>One method of the service, by name, with arguments that suit the lamp of the fixtures.</summary>
    private static async Task Invoke(IFritzBoxService service, string operation, CancellationToken token)
    {
        switch (operation)
        {
            case nameof(IFritzBoxService.FritzBoxLogin):
                await service.FritzBoxLogin(token);
                break;
            case nameof(IFritzBoxService.GetDevices):
                await service.GetDevices(token);
                break;
            case nameof(IFritzBoxService.SwitchDevice):
                await service.SwitchDevice(LampAin, true, token);
                break;
            case nameof(IFritzBoxService.SetLevel):
                await service.SetLevel(LampAin, 0.5, token);
                break;
            case nameof(IFritzBoxService.SetColorTemperature):
                await service.SetColorTemperature(LampAin, 3000, token);
                break;
            case nameof(IFritzBoxService.SetColor):
                await service.SetColor(LampAin, 120, 0.5, token);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
        }
    }

    /// <summary>
    /// A command over the REST API as it is made without a GetDevices before: the version, the overview (which the
    /// UID is looked up in) and the one PUT.
    /// </summary>
    private static void AssertRestCommand(FakeBox box, string path, JsonNode expected)
    {
        Assert.Equal([$"GET {BoxInfoPath}", $"GET {OverviewPath}", $"PUT {path}"], box.Requests.Select(c => c.Line));
        AssertPut(box, path, expected);
    }

    /// <summary>The one PUT, to <paramref name="path"/> and with the JSON <paramref name="expected"/>, which is compared as JSON and not as text.</summary>
    private static void AssertPut(FakeBox box, string path, JsonNode expected)
    {
        var put = Assert.Single(box.Puts);
        Assert.Equal(path, put.PathAndQuery);
        Assert.True(JsonNode.DeepEquals(expected, JsonNode.Parse(put.Body)), $"The box was sent {put.Body} instead of {expected.ToJsonString()}");
    }

    /// <summary>The body of a PUT to a unit: <c>{"interfaces":{"&lt;name&gt;":&lt;value&gt;}}</c>.</summary>
    private static JsonNode Interfaces(string name, JsonNode value) => new JsonObject { ["interfaces"] = new JsonObject { [name] = value } };

    private static JsonNode SwitchBody(bool turnOn) => Interfaces("onOffInterface", new JsonObject { ["active"] = turnOn });

    /// <summary>A command over AHA: the version and the one command, with the session the login gave.</summary>
    private static void AssertAhaCommand(FakeBox box, string query) =>
        Assert.Equal([$"GET {BoxInfoPath}", $"GET {AhaPath}?{query}&sid={FakeBox.Sid(1)}"], box.Requests.Select(c => c.Line));

    /// <summary>One request as the fake box received it, with the path and query as the box sees them.</summary>
    private sealed record Call(HttpMethod Method, string PathAndQuery, string? Authorization, string Body)
    {
        public string Line => $"{Method} {PathAndQuery}";

        public string Path => PathAndQuery.Split('?')[0];

        public bool IsLogin => PathAndQuery == LoginPath;

        public bool IsBoxInfo => Path == BoxInfoPath;

        public bool IsRest => Path.StartsWith("/api/v0/", StringComparison.Ordinal);

        public bool IsAha => Path == AhaPath;

        /// <summary>A request of the Smart Home REST API or of AHA, as against the login and the box info.</summary>
        public bool IsApi => IsRest || IsAha;

        public string Query => PathAndQuery.Contains('?', StringComparison.Ordinal) ? PathAndQuery[(PathAndQuery.IndexOf('?', StringComparison.Ordinal) + 1)..] : string.Empty;
    }

    /// <summary>
    /// A FRITZ!Box with a smart home: <c>jason_boxinfo.xml</c>, <c>login_sid.lua?version=2</c> (which accepts any
    /// response and gives the n-th login the session <see cref="Sid"/>), the REST overview and PUT of a unit, and the
    /// AHA interface. Every request is recorded, in the order it arrived.
    /// </summary>
    private sealed class FakeBox : HttpMessageHandler
    {
        private const string NoSession = "0000000000000000";
        private const string Challenge = "2$10$5A1711$10$5A1722";

        private static readonly (HttpStatusCode Status, string Body) noBoxInfo = (HttpStatusCode.NotFound, "Not found");

        private readonly Lock gate = new();
        private readonly List<Call> calls = [];
        private int logins;
        private int disposals;

        /// <param name="version">The <c>j:Version</c> of <c>jason_boxinfo.xml</c>, or <see langword="null"/> for a box that answers 404.</param>
        public FakeBox(string? version = Os825) => BoxInfo = version == null ? noBoxInfo : BoxInfoOf(version);

        /// <summary>The answer to <c>jason_boxinfo.xml</c>.</summary>
        public (HttpStatusCode Status, string Body) BoxInfo { get; set; }

        /// <summary>The box answers a login with no session, as it does for a wrong password.</summary>
        public bool RefuseLogin { get; init; }

        /// <summary>Runs when a request has been recorded and before it is answered. A test can hold a request with it.</summary>
        public Func<Call, CancellationToken, Task>? Before { get; init; }

        public int Disposals => Volatile.Read(ref disposals);

        public IReadOnlyList<Call> Calls
        {
            get
            {
                lock (gate)
                {
                    return [.. calls];
                }
            }
        }

        /// <summary>Everything but the login: the version, the overview, the commands.</summary>
        public IReadOnlyList<Call> Requests => [.. Calls.Where(c => !c.IsLogin)];

        public IReadOnlyList<Call> LoginGets => [.. Calls.Where(c => c.IsLogin && c.Method == HttpMethod.Get)];

        public IReadOnlyList<Call> LoginPosts => [.. Calls.Where(c => c.IsLogin && c.Method == HttpMethod.Post)];

        public IReadOnlyList<Call> BoxInfoRequests => [.. Calls.Where(c => c.IsBoxInfo)];

        public IReadOnlyList<Call> OverviewRequests => [.. Calls.Where(c => c.Method == HttpMethod.Get && c.PathAndQuery == OverviewPath)];

        public IReadOnlyList<Call> Puts => [.. Calls.Where(c => c.Method == HttpMethod.Put)];

        /// <summary>The session the n-th login (counting from 1) gets.</summary>
        public static string Sid(int login) => (0x0123456789abcdefUL + (ulong)(login - 1)).ToString("x16");

        public static (HttpStatusCode Status, string Body) BoxInfoOf(string version) =>
            (HttpStatusCode.OK, $"""<j:BoxInfo xmlns:j="http://jason.avm.de/updatecheck/"><j:Name>FRITZ!Box 5690 Pro</j:Name><j:HW>254</j:HW><j:Version>{version}</j:Version></j:BoxInfo>""");

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var call = new Call
            (
                request.Method,
                request.RequestUri!.PathAndQuery,
                request.Headers.TryGetValues("Authorization", out var authorization) ? string.Join(", ", authorization) : null,
                request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)
            );

            lock (gate)
            {
                calls.Add(call);
            }

            if (Before != null)
            {
                await Before(call, cancellationToken).ConfigureAwait(false);
            }

            return Answer(call);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Interlocked.Increment(ref disposals);
            }

            base.Dispose(disposing);
        }

        private HttpResponseMessage Answer(Call call)
        {
            if (call.Method == HttpMethod.Get && call.IsBoxInfo)
            {
                return Respond(BoxInfo.Status, BoxInfo.Body, "text/xml");
            }

            if (call.IsLogin && call.Method == HttpMethod.Get)
            {
                return SessionInfo(NoSession);
            }

            if (call.IsLogin && call.Method == HttpMethod.Post)
            {
                return SessionInfo(RefuseLogin ? NoSession : Sid(Interlocked.Increment(ref logins)));
            }

            if (call.Method == HttpMethod.Get && call.PathAndQuery == OverviewPath)
            {
                return Respond(HttpStatusCode.OK, FritzBoxSmartHomeFixtures.Overview, "application/json");
            }

            if (call.Method == HttpMethod.Put && call.PathAndQuery.StartsWith(UnitsPath, StringComparison.Ordinal))
            {
                return Respond(HttpStatusCode.OK, string.Empty, "application/json");
            }

            if (call.Method == HttpMethod.Get && call.IsAha)
            {
                var isDeviceList = HttpUtility.ParseQueryString(call.Query)["switchcmd"] == "getdevicelistinfos";
                return Respond(HttpStatusCode.OK, isDeviceList ? FritzBoxSmartHomeFixtures.DeviceList : string.Empty, "text/xml");
            }

            throw new InvalidOperationException($"The fake box knows nothing about {call.Line}");
        }

        private static HttpResponseMessage Respond(HttpStatusCode status, string body, string contentType) => new(status) { Content = new StringContent(body, Encoding.UTF8, contentType) };

        private static HttpResponseMessage SessionInfo(string sid) => Respond
        (
            HttpStatusCode.OK,
            $"""<?xml version="1.0" encoding="utf-8"?><SessionInfo><SID>{sid}</SID><Challenge>{Challenge}</Challenge><BlockTime>0</BlockTime><Rights></Rights><Users><User last="1">user</User></Users></SessionInfo>""",
            "text/xml"
        );
    }
}
