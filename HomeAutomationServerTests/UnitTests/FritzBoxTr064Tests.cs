using System.Net;
using System.Text;
using System.Xml.Linq;
using De.Hochstaetter.FritzBox.Tr064;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The TR-064 side of the FritzBox library against a fake box: the SOAP envelope, the description, the second
/// factor and adding a USP controller. The description and SCPD are cut down from what FRITZ!OS documents.
/// </summary>
public sealed class FritzBoxTr064Tests
{
    private const string Description = """
        <?xml version="1.0"?>
        <root xmlns="urn:dslforum-org:device-1-0">
        <specVersion><major>1</major><minor>0</minor></specVersion>
        <systemVersion><HW>243</HW><Major>8</Major><Minor>20</Minor><Display>254.08.20</Display></systemVersion>
        <device>
        <deviceType>urn:dslforum-org:device:InternetGatewayDevice:1</deviceType>
        <friendlyName>FRITZ!Box 7590 AX</friendlyName>
        <modelName>FRITZ!Box 7590 AX</modelName>
        <serviceList>
        <service><serviceType>urn:dslforum-org:service:DeviceInfo:1</serviceType><serviceId>urn:DeviceInfo-com:serviceId:DeviceInfo1</serviceId><controlURL>/upnp/control/deviceinfo</controlURL><SCPDURL>/deviceinfoSCPD.xml</SCPDURL></service>
        <service><serviceType>urn:dslforum-org:service:X_AVM-DE_Auth:1</serviceType><serviceId>urn:X_AVM-DE_Auth-com:serviceId:X_AVM-DE_Auth1</serviceId><controlURL>/upnp/control/x_auth</controlURL><SCPDURL>/x_authSCPD.xml</SCPDURL></service>
        <service><serviceType>urn:dslforum-org:service:X_AVM-DE_USPController:1</serviceType><serviceId>urn:X_AVM-DE_USPController-com:serviceId:X_AVM-DE_USPController1</serviceId><controlURL>/upnp/control/x_uspcontroller</controlURL><SCPDURL>/x_uspcontrollerSCPD.xml</SCPDURL></service>
        </serviceList>
        </device>
        </root>
        """;

    [Fact]
    public void The_description_names_the_model_the_firmware_and_finds_a_service_by_the_name_in_its_type()
    {
        var description = Tr064Description.Parse(Description);

        Assert.Equal("FRITZ!Box 7590 AX", description.ModelName);
        Assert.Equal("254.08.20", description.SoftwareVersion);
        Assert.Equal(3, description.Services.Count);
        Assert.Equal("/upnp/control/x_uspcontroller", description.FindService("X_AVM-DE_USPController")?.ControlUrl);
        Assert.Null(description.FindService("USPController"));
    }

    [Fact]
    public void A_request_puts_the_arguments_unqualified_into_the_action_and_the_second_factor_token_into_the_header()
    {
        var xml = Tr064Soap.CreateRequest("urn:dslforum-org:service:X_AVM-DE_USPController:1", "DeleteUSPControllerByIndex", [new("NewIndex", "2")], "2C0A2110-30BA-444e-8B83-566BC3F19C80");
        XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
        XNamespace service = "urn:dslforum-org:service:X_AVM-DE_USPController:1";

        var envelope = XDocument.Parse(xml).Root!;
        var token = envelope.Element(soap + "Header")!.Element((XNamespace)"avm.de" + "token")!;
        var action = envelope.Element(soap + "Body")!.Element(service + "DeleteUSPControllerByIndex")!;

        Assert.Equal("2C0A2110-30BA-444e-8B83-566BC3F19C80", token.Value);
        Assert.Equal("1", token.Attribute(soap + "mustUnderstand")?.Value);
        Assert.Equal("2", action.Element("NewIndex")?.Value);
    }

    [Fact]
    public void A_request_without_second_factor_has_no_header()
    {
        var xml = Tr064Soap.CreateRequest("urn:dslforum-org:service:DeviceInfo:1", "GetInfo", []);

        Assert.Null(XDocument.Parse(xml).Root!.Element((XNamespace)"http://schemas.xmlsoap.org/soap/envelope/" + "Header"));
    }

    [Fact]
    public void A_fault_becomes_a_Tr064Exception_with_the_UPnP_error_code()
    {
        var exception = Assert.Throws<Tr064Exception>(() => Tr064Soap.ParseResponse(FakeBox.Fault(866, "second factor authentication required"), "AddUSPController"));

        Assert.Equal(Tr064Exception.SecondFactorRequired, exception.ErrorCode);
        Assert.Equal("AddUSPController", exception.Action);
        Assert.Equal("second factor authentication required", exception.ErrorDescription);
    }

    [Fact]
    public void A_response_yields_its_output_arguments_by_name()
    {
        var values = Tr064Soap.ParseResponse(FakeBox.Response("GetInfo", new() { ["NewEnabled"] = "1" }), "GetInfo");

        Assert.Equal("1", Assert.Single(values).Value);
        Assert.True(Tr064Soap.ParseBoolean(values["NewEnabled"]));
    }

    [Fact]
    public async Task Connecting_with_TLS_sends_the_actions_to_the_security_port_over_HTTPS()
    {
        var box = new FakeBox((action, _) => action switch
        {
            "GetSecurityPort" => FakeBox.Response(action, new() { ["NewSecurityPort"] = "49443" }),
            _ => FakeBox.Response(action, new() { ["NewEnabled"] = "0" }),
        });

        using var client = box.CreateClient(useTls: true);
        await client.ConnectAsync(TestContext.Current.CancellationToken);
        await client.InvokeAsync("X_AVM-DE_Auth", "GetInfo", [], TestContext.Current.CancellationToken);

        Assert.Equal("http://192.168.44.11:49000/upnp/control/deviceinfo", box.Calls[0].Uri.ToString());
        Assert.Equal("https://192.168.44.11:49443/upnp/control/x_auth", box.Calls[1].Uri.ToString());
    }

    [Fact]
    public async Task Adding_a_controller_waits_for_the_second_factor_and_repeats_the_action_with_the_token()
    {
        var states = new Queue<string>(["waitingforauth", "authenticated"]);
        var addCalls = 0;

        var box = new FakeBox((action, _) => action switch
        {
            "AddUSPController" when addCalls++ == 0 => FakeBox.Fault(Tr064Exception.SecondFactorRequired, "second factor authentication required"),
            "AddUSPController" => FakeBox.Response(action, new() { ["NewIndex"] = "2" }),
            "SetConfig" => FakeBox.Response(action, new() { ["NewToken"] = "the-token", ["NewState"] = "waitingforauth", ["NewMethods"] = "button,dtmf;*11234" }),
            "GetState" => FakeBox.Response(action, new() { ["NewState"] = states.Dequeue() }),
            _ => throw new InvalidOperationException(action),
        });

        using var client = box.CreateClient();
        await client.ConnectAsync(TestContext.Current.CancellationToken);
        string? methods = null;

        var index = await new UspControllerRegistry(client).AddAsync
        (
            new UspControllerEntry
            {
                EndpointId = "proto::ha-server",
                Hostname = "192.168.44.50",
                MqttControllerTopic = "usp/controller/ha-server",
                MqttResponseTopic = "usp/agent/fritzbox",
                Username = "fritzbox",
                AccessRights = UspAccessRights.Smarthome | UspAccessRights.Internet,
            },
            "secret",
            (m, _) =>
            {
                methods = m;
                return Task.CompletedTask;
            },
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(2, index);
        Assert.Equal("button,dtmf;*11234", methods);
        Assert.Equal(["AddUSPController", "SetConfig", "GetState", "GetState", "AddUSPController"], box.Calls.Select(c => c.Action));
        Assert.DoesNotContain("the-token", box.Calls[0].Body);
        Assert.All(box.Calls.Skip(2), call => Assert.Contains(">the-token</avm:token>", call.Body));

        var arguments = FakeBox.Arguments(box.Calls[^1].Body);
        Assert.Equal("1", arguments["NewAccessRightSmarthome"]);
        Assert.Equal("1", arguments["NewAccessRightInternet"]);
        Assert.Equal("0", arguments["NewAccessRightVoIP"]);
        Assert.Equal("mqtt", arguments["NewMTP"]);
        Assert.Equal("secret", arguments["NewPassword"]);

        // The SCPD of the fake box does not declare it, and the box would answer an undeclared argument with 402.
        Assert.False(arguments.ContainsKey("NewMQTTClientID"));
    }

    [Fact]
    public async Task A_box_that_declares_no_MQTT_topics_cannot_take_an_MQTT_controller()
    {
        var box = new FakeBox((action, _) => throw new InvalidOperationException(action)) { WithoutMqtt = true };
        using var client = box.CreateClient();
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<NotSupportedException>(() => new UspControllerRegistry(client).AddAsync
        (
            new UspControllerEntry { EndpointId = "proto::ha-server", Hostname = "192.168.44.50" },
            "secret",
            (_, _) => Task.CompletedTask,
            TimeSpan.FromSeconds(1),
            TestContext.Current.CancellationToken
        ));

        Assert.Empty(box.Calls);
    }

    [Fact]
    public async Task An_argument_that_breaks_the_limits_of_the_box_is_named_before_anything_is_sent()
    {
        var box = new FakeBox((action, _) => throw new InvalidOperationException(action))
        {
            Limits = new() { ["NewMinCharsPassword"] = "8", ["NewMaxCharsPassword"] = "256" },
        };

        using var client = box.CreateClient();
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => new UspControllerRegistry(client).AddAsync
        (
            new UspControllerEntry { EndpointId = "proto::ha-server", Hostname = "192.168.44.50" },
            "secret",
            (_, _) => Task.CompletedTask,
            TimeSpan.FromSeconds(1),
            TestContext.Current.CancellationToken
        ));

        Assert.Empty(box.Calls);
        Assert.Contains("NewPassword", exception.Message);
    }

    [Theory]
    [InlineData("NewEndpointID", "proto::ha-server", "NewAllowedCharsEndpointID", "abcdefghijklmnopqrstuvwxyz-", "':'")]
    [InlineData("NewPassword", "short", "NewMinCharsPassword", "8", "at least 8")]
    [InlineData("NewHostname", "192.168.44.50", "NewMaxCharsHostname", "5", "at most 5")]
    public void Validation_names_the_argument_and_the_limit_it_breaks(string name, string value, string limit, string limitValue, string expected)
    {
        var exception = Assert.Throws<ArgumentException>(() => UspControllerRegistry.Validate(new Dictionary<string, string> { [name] = value }, new Dictionary<string, string> { [limit] = limitValue }));

        Assert.Contains(name, exception.Message);
        Assert.Contains(expected, exception.Message);
    }

    [Fact]
    public void Validation_lets_through_what_the_limits_allow_and_ignores_arguments_without_limits()
    {
        UspControllerRegistry.Validate
        (
            new Dictionary<string, string> { ["NewEndpointID"] = "proto::ha-server", ["NewPort"] = "1883", ["NewPath"] = string.Empty },
            new Dictionary<string, string>
            {
                ["NewMinCharsEndpointID"] = "1",
                ["NewMaxCharsEndpointID"] = "64",
                ["NewAllowedCharsEndpointID"] = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz:-._%",
                ["NewMinCharsPath"] = "0",
            }
        );
    }

    [Fact]
    public async Task A_second_factor_that_gets_blocked_ends_the_action()
    {
        var box = new FakeBox((action, _) => action switch
        {
            "AddUSPController" => FakeBox.Fault(Tr064Exception.SecondFactorRequired, "second factor authentication required"),
            "SetConfig" => FakeBox.Response(action, new() { ["NewToken"] = "t", ["NewState"] = "waitingforauth", ["NewMethods"] = "button" }),
            "GetState" => FakeBox.Response(action, new() { ["NewState"] = "blocked" }),
            _ => throw new InvalidOperationException(action),
        });

        using var client = box.CreateClient();
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new UspControllerRegistry(client).AddAsync
        (
            new UspControllerEntry { EndpointId = "proto::ha-server", Hostname = "192.168.44.50" },
            "secret",
            (_, _) => Task.CompletedTask,
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken
        ));

        Assert.Equal(1, box.Calls.Count(c => c.Action == "AddUSPController"));
    }

    private sealed class FakeBox(Func<string, string, string> respond) : HttpMessageHandler
    {
        private const string UspControllerScpd = """
            <?xml version="1.0"?>
            <scpd xmlns="urn:dslforum-org:service-1-0">
            <actionList>
            <action><name>AddUSPController</name><argumentList>
            {0}
            <argument><name>NewIndex</name><direction>out</direction></argument>
            </argumentList></action>
            </actionList>
            </scpd>
            """;

        private static readonly string[] commonArguments =
        [
            "NewEnable", "NewEndpointID", "NewMTP", "NewHostname", "NewPath", "NewPort", "NewUseTLS", "NewAccessRightSmarthome", "NewAccessRightMesh",
            "NewAccessRightInternet", "NewAccessRightSystem", "NewAccessRightController", "NewAccessRightWiFi", "NewAccessRightVoIP", "NewUsername", "NewPassword",
        ];

        public List<(Uri Uri, string Action, string Body)> Calls { get; } = [];

        public bool WithoutMqtt { get; init; }

        /// <summary>What <c>X_AVM-DE_USPController:GetInfo</c> answers. Not recorded in <see cref="Calls"/>.</summary>
        public Dictionary<string, string> Limits { get; init; } = new() { ["NewMinCharsEndpointID"] = "1", ["NewMaxCharsEndpointID"] = "64" };

        public Tr064Client CreateClient(bool useTls = false) => new
        (
            new Tr064ClientOptions { BaseUri = new Uri("http://192.168.44.11:49000"), UseTls = useTls, SecondFactorPollInterval = TimeSpan.FromMilliseconds(1) },
            null,
            this
        );

        public static string Response(string action, Dictionary<string, string> values) =>
            $"""<?xml version="1.0"?><s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/"><s:Body><u:{action}Response xmlns:u="urn:dslforum-org:service:Any:1">{string.Concat(values.Select(v => $"<{v.Key}>{v.Value}</{v.Key}>"))}</u:{action}Response></s:Body></s:Envelope>""";

        public static string Fault(int code, string description) =>
            $"""<?xml version="1.0"?><s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/"><s:Body><s:Fault><faultcode>s:Client</faultcode><faultstring>UPnPError</faultstring><detail><UPnPError xmlns="urn:dslforum-org:control-1-0"><errorCode>{code}</errorCode><errorDescription>{description}</errorDescription></UPnPError></detail></s:Fault></s:Body></s:Envelope>""";

        public static Dictionary<string, string> Arguments(string body) =>
            XDocument.Parse(body).Descendants().Single(e => e.Name.LocalName == "AddUSPController").Elements().ToDictionary(e => e.Name.LocalName, e => e.Value);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get)
            {
                var arguments = WithoutMqtt ? commonArguments : [.. commonArguments, "NewMQTTControllerTopic", "NewMQTTResponseTopic"];

                return Ok(request.RequestUri!.AbsolutePath switch
                {
                    "/tr64desc.xml" => Description,
                    "/x_uspcontrollerSCPD.xml" => string.Format(UspControllerScpd, string.Concat(arguments.Select(a => $"<argument><name>{a}</name><direction>in</direction></argument>"))),
                    var path => throw new InvalidOperationException(path),
                });
            }

            var action = request.Headers.GetValues("SOAPACTION").Single().Trim('"').Split('#')[1];
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);

            if (action == "GetInfo" && request.RequestUri!.AbsolutePath == "/upnp/control/x_uspcontroller")
            {
                return Ok(Response(action, Limits));
            }

            Calls.Add((request.RequestUri!, action, body));
            var answer = respond(action, body);

            return answer.Contains("<s:Fault>", StringComparison.Ordinal)
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent(answer, Encoding.UTF8, "text/xml") }
                : Ok(answer);
        }

        private static HttpResponseMessage Ok(string content) => new(HttpStatusCode.OK) { Content = new StringContent(content, Encoding.UTF8, "text/xml") };
    }
}
