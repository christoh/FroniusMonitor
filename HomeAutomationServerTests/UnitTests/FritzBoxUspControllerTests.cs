using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using De.Hochstaetter.FritzBox.Usp;
using Google.Protobuf;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Usp;
using Record = UspRecord.Record;
using MQTTConnectRecord = UspRecord.MQTTConnectRecord;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The USP controller of the FritzBox library talking to a fake box over an MQTT broker that runs in the test.
/// </summary>
public sealed class FritzBoxUspControllerTests : IAsyncLifetime
{
    private const string AgentId = "os::box";
    private const string AgentTopic = "usp/agent/box";
    private const string ControllerId = "proto::test";
    private const string ControllerTopic = "usp/controller/test";

    private readonly ILoggerFactory loggers = LoggerFactory.Create(builder => builder.AddTestOutput());
    private readonly Channel<MqttApplicationMessage> receivedByAgent = Channel.CreateUnbounded<MqttApplicationMessage>();
    private MqttServer? broker;
    private IMqttClient? agent;
    private UspMqttController? controller;
    private int port;

    [Fact]
    public async Task The_controller_learns_the_box_from_its_connect_record_and_gets_the_answer_to_its_request()
    {
        await IntroduceAgentAsync();

        var response = await controller!.GetAsync(["Device.DeviceInfo."], token: TestContext.Current.CancellationToken);

        Assert.Equal(AgentId, controller.AgentEndpointId);
        Assert.Equal(AgentTopic, controller.AgentTopic);
        var result = Assert.Single(Assert.Single(response.ReqPathResults).ResolvedPathResults);
        Assert.Equal("FRITZ!Box 7590 AX", result.ResultParams["ModelName"]);

        var request = await receivedByAgent.Reader.ReadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(UspCodec.ContentType, request.ContentType);
        Assert.Equal(ControllerTopic, request.ResponseTopic);
    }

    [Fact]
    public async Task An_error_from_the_box_becomes_a_UspException()
    {
        await IntroduceAgentAsync();

        var exception = await Assert.ThrowsAsync<UspException>(() => controller!.GetAsync(["Device.Nothing."], token: TestContext.Current.CancellationToken));

        Assert.Equal(7026u, exception.ErrorCode);
    }

    [Fact]
    public async Task A_notification_that_asks_for_an_answer_is_passed_on_and_answered_with_its_own_message_id()
    {
        await IntroduceAgentAsync();
        var notified = new TaskCompletionSource<Notify>(TaskCreationOptions.RunContinuationsAsynchronously);
        controller!.NotificationReceived += (_, e) => notified.TrySetResult(e.Notify);

        var notify = new Msg
        {
            Header = new Header { MsgId = "n-1", MsgType = Header.Types.MsgType.Notify },
            Body = new Body
            {
                Request = new Request
                {
                    Notify = new Notify
                    {
                        SubscriptionId = "switches",
                        SendResp = true,
                        ValueChange = new Notify.Types.ValueChange { ParamPath = "Device.Outlet.1.On", ParamValue = "true" },
                    },
                },
            },
        };

        await PublishFromAgentAsync(UspCodec.Wrap(notify, "1.3", ControllerId, AgentId));

        var received = await notified.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.Equal("Device.Outlet.1.On", received.ValueChange.ParamPath);

        var answer = UspCodec.GetMessage(UspCodec.ParseRecord((await receivedByAgent.Reader.ReadAsync(TestContext.Current.CancellationToken)).Payload.ToArray()))!;
        Assert.Equal(Header.Types.MsgType.NotifyResp, answer.Header.MsgType);
        Assert.Equal("n-1", answer.Header.MsgId);
        Assert.Equal("switches", answer.Body.Response.NotifyResp.SubscriptionId);
    }

    public async ValueTask InitializeAsync()
    {
        port = FreePort();
        var serverFactory = new MqttServerFactory();
        broker = serverFactory.CreateMqttServer(serverFactory.CreateServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointBoundIPAddress(IPAddress.Loopback)
            .WithDefaultEndpointBoundIPV6Address(IPAddress.None) // not every build machine has IPv6
            .WithDefaultEndpointPort(port)
            .Build());
        await broker.StartAsync();

        agent = new MqttClientFactory().CreateMqttClient();
        agent.ApplicationMessageReceivedAsync += AnswerAsAgentAsync;
        await agent.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", port).WithClientId("box").WithProtocolVersion(MqttProtocolVersion.V500).Build());
        await agent.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter(AgentTopic, MqttQualityOfServiceLevel.AtLeastOnce).Build());

        controller = new UspMqttController
        (
            new UspMqttControllerOptions
            {
                BrokerHost = "127.0.0.1",
                BrokerPort = port,
                EndpointId = ControllerId,
                ControllerTopic = ControllerTopic,
                RequestTimeout = TimeSpan.FromSeconds(10),
            },
            loggers.CreateLogger<UspMqttController>()
        );

        await controller.ConnectAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (controller != null)
        {
            await controller.DisposeAsync();
        }

        agent?.Dispose();

        if (broker != null)
        {
            await broker.StopAsync();
            broker.Dispose();
        }

        loggers.Dispose();
    }

    /// <summary>What a box does after connecting to the broker: it sends each controller an MQTT Connect Record.</summary>
    private async Task IntroduceAgentAsync()
    {
        var connect = new Record
        {
            Version = "1.3",
            ToId = ControllerId,
            FromId = AgentId,
            MqttConnect = new MQTTConnectRecord { Version = MQTTConnectRecord.Types.MQTTVersion.V5, SubscribedTopic = AgentTopic },
        };

        await PublishFromAgentAsync(connect.ToByteArray());
        await controller!.WaitForAgentAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    }

    private Task PublishFromAgentAsync(byte[] payload) => agent!.PublishAsync
    (
        new MqttApplicationMessageBuilder()
            .WithTopic(ControllerTopic)
            .WithPayload(payload)
            .WithContentType(UspCodec.ContentType)
            .WithResponseTopic(AgentTopic)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build(),
        TestContext.Current.CancellationToken
    );

    /// <summary>The fake box answers Get with a model name, and anything below Device.Nothing. with an error.</summary>
    private async Task AnswerAsAgentAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var message = e.ApplicationMessage;
        await receivedByAgent.Writer.WriteAsync(message);
        var request = UspCodec.GetMessage(UspCodec.ParseRecord(message.Payload.ToArray()));

        if (request?.Body.Request?.Get is not { } get)
        {
            return;
        }

        var answer = new Msg { Header = new Header { MsgId = request.Header.MsgId } };

        if (get.ParamPaths.Contains("Device.Nothing."))
        {
            answer.Header.MsgType = Header.Types.MsgType.Error;
            answer.Body = new Body { Error = new Error { ErrCode = 7026, ErrMsg = "Invalid path" } };
        }
        else
        {
            answer.Header.MsgType = Header.Types.MsgType.GetResp;
            var resolved = new GetResp.Types.ResolvedPathResult { ResolvedPath = "Device.DeviceInfo.", ResultParams = { ["ModelName"] = "FRITZ!Box 7590 AX" } };
            answer.Body = new Body { Response = new Response { GetResp = new GetResp { ReqPathResults = { new GetResp.Types.RequestedPathResult { RequestedPath = "Device.DeviceInfo.", ResolvedPathResults = { resolved } } } } } };
        }

        await agent!.PublishAsync
        (
            new MqttApplicationMessageBuilder()
                .WithTopic(message.ResponseTopic)
                .WithPayload(UspCodec.Wrap(answer, "1.3", ControllerId, AgentId))
                .WithContentType(UspCodec.ContentType)
                .WithResponseTopic(AgentTopic)
                .Build()
        );
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
