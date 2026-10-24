using System.Buffers;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace De.Hochstaetter.FritzBox.Usp;

public sealed class UspMqttControllerOptions
{
    public required string BrokerHost { get; init; }

    public int BrokerPort { get; init; } = 1883;

    public bool UseTls { get; init; }

    public string? UserName { get; init; }

    public string? Password { get; init; }

    /// <summary>The MQTT client id. Defaults to <see cref="EndpointId"/> with ":" replaced.</summary>
    public string? ClientId { get; init; }

    /// <summary>Our USP Endpoint ID, the one the box has for this controller.</summary>
    public required string EndpointId { get; init; }

    /// <summary>
    /// Where the box sends to us. Subscribed to, and sent as our "reply to" topic with every request.
    /// </summary>
    public required string ControllerTopic { get; init; }

    /// <summary>
    /// The box's Endpoint ID and topic, if known beforehand. Otherwise they are taken from the MQTT Connect Record
    /// the box sends after it connected, or from the first Record it sends us.
    /// </summary>
    public string? AgentEndpointId { get; init; }

    /// <inheritdoc cref="AgentEndpointId"/>
    public string? AgentTopic { get; init; }

    /// <summary>The USP version written into our Records, until the box has shown which one it uses.</summary>
    public string RecordVersion { get; init; } = "1.3";

    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>More topic filters to subscribe to, to see what the box publishes where (needs the broker's permission).</summary>
    public IReadOnlyList<string> AdditionalTopicFilters { get; init; } = [];
}

/// <summary>Every MQTT message that arrived, decoded as far as possible.</summary>
public sealed class UspRecordEventArgs(string topic, string? replyTo, string? contentType, Record? record, Msg? message, Exception? error) : EventArgs
{
    public DateTimeOffset Received { get; } = DateTimeOffset.UtcNow;

    public string Topic { get; } = topic;

    public string? ReplyTo { get; } = replyTo;

    public string? ContentType { get; } = contentType;

    public Record? Record { get; } = record;

    public Msg? Message { get; } = message;

    /// <summary>Why the payload could not be decoded, or <see langword="null"/>.</summary>
    public Exception? Error { get; } = error;
}

public sealed class UspNotificationEventArgs(Msg message) : EventArgs
{
    public DateTimeOffset Received { get; } = DateTimeOffset.UtcNow;

    public Msg Message { get; } = message;

    public Notify Notify => Message.Body.Request.Notify;
}

/// <summary>
/// A USP Controller (TR-369) that talks to the box over an MQTT broker: it sends requests and waits for the
/// matching responses, and passes on the notifications of subscriptions.
/// </summary>
public sealed class UspMqttController : IAsyncDisposable
{
    private readonly UspMqttControllerOptions options;
    private readonly ILogger logger;
    private readonly IMqttClient client;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<Msg>> pendingRequests = new();
    private readonly TaskCompletionSource agentKnown = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private string recordVersion;

    public UspMqttController(UspMqttControllerOptions options, ILogger<UspMqttController>? logger = null)
    {
        this.options = options;
        this.logger = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        client = new MqttClientFactory().CreateMqttClient();
        client.ApplicationMessageReceivedAsync += OnApplicationMessageReceivedAsync;
        recordVersion = options.RecordVersion;
        AgentEndpointId = options.AgentEndpointId;
        AgentTopic = options.AgentTopic;

        if (AgentEndpointId != null && AgentTopic != null)
        {
            agentKnown.TrySetResult();
        }
    }

    public string? AgentEndpointId { get; private set; }

    public string? AgentTopic { get; private set; }

    /// <summary>The MQTT version the box said it uses in its Connect Record, if it sent one.</summary>
    public MQTTConnectRecord.Types.MQTTVersion? AgentMqttVersion { get; private set; }

    public event EventHandler<UspRecordEventArgs>? RecordReceived;

    public event EventHandler<UspNotificationEventArgs>? NotificationReceived;

    public async Task ConnectAsync(CancellationToken token = default)
    {
        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer(options.BrokerHost, options.BrokerPort)
            .WithClientId(options.ClientId ?? options.EndpointId.Replace(':', '-'))
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .WithUserProperty("usp-endpoint-id", new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(options.EndpointId)));

        if (options.UserName != null)
        {
            builder.WithCredentials(options.UserName, options.Password);
        }

        if (options.UseTls)
        {
            builder.WithTlsOptions(tls => tls.UseTls());
        }

        await client.ConnectAsync(builder.Build(), token).ConfigureAwait(false);

        var subscription = new MqttClientSubscribeOptionsBuilder();

        // A broker may deliver a message once per matching subscription, so our own topic is subscribed to only if no
        // additional filter covers it already.
        if (!options.AdditionalTopicFilters.Any(filter => MqttTopicFilterComparer.Compare(options.ControllerTopic, filter) == MqttTopicFilterCompareResult.IsMatch))
        {
            subscription.WithTopicFilter(options.ControllerTopic, MqttQualityOfServiceLevel.AtLeastOnce);
        }

        foreach (var filter in options.AdditionalTopicFilters)
        {
            subscription.WithTopicFilter(filter, MqttQualityOfServiceLevel.AtLeastOnce);
        }

        await client.SubscribeAsync(subscription.Build(), token).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation
            (
                "USP: {EndpointId} connected to {Host}:{Port}, listening on {Topics}",
                options.EndpointId, options.BrokerHost, options.BrokerPort, string.Join(", ", [options.ControllerTopic, .. options.AdditionalTopicFilters])
            );
        }
    }

    /// <summary>Waits until the box's Endpoint ID and topic are known.</summary>
    public Task WaitForAgentAsync(TimeSpan timeout, CancellationToken token = default) => agentKnown.Task.WaitAsync(timeout, token);

    /// <summary>
    /// Sends <paramref name="request"/> to the box and returns its response.
    /// </summary>
    /// <exception cref="UspException">The box answered with an Error.</exception>
    /// <exception cref="TimeoutException">No answer within <see cref="UspMqttControllerOptions.RequestTimeout"/>.</exception>
    public async Task<Response> SendAsync(Request request, CancellationToken token = default)
    {
        var agentId = AgentEndpointId ?? throw new InvalidOperationException("The box has not introduced itself yet");
        var agentTopic = AgentTopic ?? throw new InvalidOperationException("The box's topic is not known yet");
        var message = UspCodec.CreateRequest(request);
        var completion = new TaskCompletionSource<Msg>(TaskCreationOptions.RunContinuationsAsynchronously);
        pendingRequests[message.Header.MsgId] = completion;

        try
        {
            await PublishAsync(agentTopic, UspCodec.Wrap(message, recordVersion, agentId, options.EndpointId), token).ConfigureAwait(false);
            var answer = await completion.Task.WaitAsync(options.RequestTimeout, token).ConfigureAwait(false);

            return answer.Body.MsgBodyCase == Body.MsgBodyOneofCase.Error
                ? throw new UspException(answer.Body.Error)
                : answer.Body.Response;
        }
        finally
        {
            pendingRequests.TryRemove(message.Header.MsgId, out _);
        }
    }

    public async Task<GetSupportedProtocolResp> GetSupportedProtocolAsync(string versions = "1.0,1.1,1.2,1.3,1.4,1.5", CancellationToken token = default) =>
        (await SendAsync(new Request { GetSupportedProtocol = new GetSupportedProtocol { ControllerSupportedProtocolVersions = versions } }, token).ConfigureAwait(false))
        .GetSupportedProtocolResp;

    public async Task<GetSupportedDMResp> GetSupportedDmAsync(IEnumerable<string> objectPaths, bool firstLevelOnly = false, CancellationToken token = default)
    {
        var request = new GetSupportedDM
        {
            FirstLevelOnly = firstLevelOnly,
            ReturnCommands = true,
            ReturnEvents = true,
            ReturnParams = true,
            ReturnUniqueKeySets = true,
        };

        request.ObjPaths.AddRange(objectPaths);
        return (await SendAsync(new Request { GetSupportedDm = request }, token).ConfigureAwait(false)).GetSupportedDmResp;
    }

    public async Task<GetInstancesResp> GetInstancesAsync(IEnumerable<string> objectPaths, bool firstLevelOnly = false, CancellationToken token = default)
    {
        var request = new GetInstances { FirstLevelOnly = firstLevelOnly };
        request.ObjPaths.AddRange(objectPaths);
        return (await SendAsync(new Request { GetInstances = request }, token).ConfigureAwait(false)).GetInstancesResp;
    }

    /// <param name="maxDepth">0 for everything below the paths.</param>
    public async Task<GetResp> GetAsync(IEnumerable<string> paths, uint maxDepth = 0, CancellationToken token = default)
    {
        var request = new Get { MaxDepth = maxDepth };
        request.ParamPaths.AddRange(paths);
        return (await SendAsync(new Request { Get = request }, token).ConfigureAwait(false)).GetResp;
    }

    public async Task<SetResp> SetAsync(string objectPath, IReadOnlyDictionary<string, string> parameters, CancellationToken token = default)
    {
        var update = new Set.Types.UpdateObject { ObjPath = objectPath };
        update.ParamSettings.AddRange(parameters.Select(p => new Set.Types.UpdateParamSetting { Param = p.Key, Value = p.Value, Required = true }));
        var request = new Set { AllowPartial = false };
        request.UpdateObjs.Add(update);
        return (await SendAsync(new Request { Set = request }, token).ConfigureAwait(false)).SetResp;
    }

    public async Task<AddResp> AddAsync(string objectPath, IReadOnlyDictionary<string, string> parameters, CancellationToken token = default)
    {
        var create = new Add.Types.CreateObject { ObjPath = objectPath };
        create.ParamSettings.AddRange(parameters.Select(p => new Add.Types.CreateParamSetting { Param = p.Key, Value = p.Value, Required = true }));
        var request = new Add { AllowPartial = false };
        request.CreateObjs.Add(create);
        return (await SendAsync(new Request { Add = request }, token).ConfigureAwait(false)).AddResp;
    }

    public async Task<DeleteResp> DeleteAsync(IEnumerable<string> objectPaths, CancellationToken token = default)
    {
        var request = new Delete { AllowPartial = true };
        request.ObjPaths.AddRange(objectPaths);
        return (await SendAsync(new Request { Delete = request }, token).ConfigureAwait(false)).DeleteResp;
    }

    /// <summary>
    /// Creates a subscription in <c>Device.LocalAgent.Subscription.</c> that notifies this controller when
    /// <paramref name="path"/> changes, and returns the path of the new instance.
    /// </summary>
    /// <remarks>
    /// One path per subscription: FRITZ!OS 8.25 refuses a ReferenceList with more than one entry (7008), wildcards
    /// (7008) and object paths (7807).
    /// </remarks>
    /// <param name="persistent">Whether the subscription survives a reboot of the box.</param>
    public async Task<string> SubscribeAsync(string id, string notificationType, string path, bool persistent = false, CancellationToken token = default)
    {
        var response = await AddAsync
        (
            "Device.LocalAgent.Subscription.",
            new Dictionary<string, string>
            {
                ["Enable"] = "true",
                ["ID"] = id,
                ["NotifType"] = notificationType,
                ["ReferenceList"] = path,
                ["Persistent"] = persistent ? "true" : "false",
            },
            token
        ).ConfigureAwait(false);

        var status = response.CreatedObjResults.Single().OperStatus;

        return status.OperStatusCase == AddResp.Types.CreatedObjectResult.Types.OperationStatus.OperStatusOneofCase.OperSuccess
            ? status.OperSuccess.InstantiatedPath
            : throw new InvalidOperationException($"The box refused the subscription: {status.OperFailure.ErrCode} {status.OperFailure.ErrMsg}");
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "USP: disconnecting from the broker failed");
            }
        }

        client.Dispose();
    }

    private async Task PublishAsync(string topic, byte[] payload, CancellationToken token)
    {
        // An MQTT 3.1.1 box sees no Response Topic property and expects our "reply to" topic in the topic name.
        var publishTopic = AgentMqttVersion == MQTTConnectRecord.Types.MQTTVersion.V311 ? UspCodec.AppendReplyTo(topic, options.ControllerTopic) : topic;

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(publishTopic)
            .WithPayload(payload)
            .WithContentType(UspCodec.ContentType)
            .WithResponseTopic(options.ControllerTopic)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await client.PublishAsync(message, token).ConfigureAwait(false);
    }

    private async Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var mqttMessage = e.ApplicationMessage;
        var replyTo = mqttMessage.ResponseTopic ?? UspCodec.GetReplyTo(mqttMessage.Topic);
        Record? record = null;
        Msg? message = null;

        try
        {
            record = UspCodec.ParseRecord(mqttMessage.Payload.ToArray());
            message = UspCodec.GetMessage(record);
        }
        catch (InvalidProtocolBufferException ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "USP: a message on {Topic} is no USP Record", mqttMessage.Topic);
            }

            RecordReceived?.Invoke(this, new UspRecordEventArgs(mqttMessage.Topic, replyTo, mqttMessage.ContentType, record, null, ex));
            return;
        }

        RecordReceived?.Invoke(this, new UspRecordEventArgs(mqttMessage.Topic, replyTo, mqttMessage.ContentType, record, message, null));

        // With additional topic filters we also see what others send, our own requests included.
        if (record.FromId == options.EndpointId || record.ToId != options.EndpointId)
        {
            return;
        }

        // The box sends a Disconnect Record when its connection goes down, for example while the controller is switched
        // off and on. It must not count as the box having introduced itself, or the first request goes nowhere.
        if (record.RecordTypeCase == Record.RecordTypeOneofCase.Disconnect)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("USP: {AgentId} disconnects: {Reason}", record.FromId, record.Disconnect.Reason);
            }

            return;
        }

        if (record.RecordTypeCase == Record.RecordTypeOneofCase.MqttConnect)
        {
            AgentMqttVersion = record.MqttConnect.Version;
            LearnAgent(record, record.MqttConnect.SubscribedTopic);
            return;
        }

        LearnAgent(record, replyTo);

        if (message == null)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("USP: {AgentId} sent a {RecordType} record", record.FromId, record.RecordTypeCase);
            }

            return;
        }

        switch (message.Body.MsgBodyCase)
        {
            case Body.MsgBodyOneofCase.Response or Body.MsgBodyOneofCase.Error:
                if (pendingRequests.TryGetValue(message.Header.MsgId, out var completion))
                {
                    completion.TrySetResult(message);
                }
                else if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("USP: {MsgType} {MsgId} answers no open request", message.Header.MsgType, message.Header.MsgId);
                }

                break;

            case Body.MsgBodyOneofCase.Request when message.Body.Request.ReqTypeCase == Request.ReqTypeOneofCase.Notify:
                await OnNotifyAsync(message, replyTo).ConfigureAwait(false);
                break;

            default:
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("USP: {AgentId} sent an unexpected {MsgType}", record.FromId, message.Header.MsgType);
                }

                break;
        }
    }

    private async Task OnNotifyAsync(Msg message, string? replyTo)
    {
        try
        {
            NotificationReceived?.Invoke(this, new UspNotificationEventArgs(message));
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "USP: handling notification {MsgId} failed", message.Header.MsgId);
            }
        }

        if (!message.Body.Request.Notify.SendResp)
        {
            return;
        }

        try
        {
            var topic = replyTo ?? AgentTopic ?? throw new InvalidOperationException("No topic to answer the notification on");
            var answer = UspCodec.CreateNotifyResponse(message);
            await PublishAsync(topic, UspCodec.Wrap(answer, recordVersion, AgentEndpointId!, options.EndpointId), CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "USP: answering notification {MsgId} failed", message.Header.MsgId);
            }
        }
    }

    private void LearnAgent(Record record, string? topic)
    {
        if (!string.IsNullOrEmpty(record.Version))
        {
            recordVersion = record.Version;
        }

        AgentEndpointId ??= record.FromId;

        if (!string.IsNullOrEmpty(topic) && AgentTopic != topic)
        {
            AgentTopic = topic;

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("USP: the box is {AgentId} on topic {Topic}, USP {Version}", AgentEndpointId, AgentTopic, recordVersion);
            }
        }

        if (AgentTopic != null)
        {
            agentKnown.TrySetResult();
        }
    }
}
