using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace De.Hochstaetter.FritzBoxUspProbe;

/// <summary>
/// The commands of the probe. Everything it learns goes to the console and, as JSON, to a folder per run below
/// <see cref="ProbeSettings.OutputDirectory"/>.
/// </summary>
internal sealed class Probe
{
    private const string UspControllerService = "X_AVM-DE_USPController";

    private static readonly JsonFormatter protobufJson = new(JsonFormatter.Settings.Default.WithFormatDefaultValues(true));
    private static readonly JsonSerializerOptions indented = new() { WriteIndented = true };
    private static readonly string[] interestingWords = ["X_AVM", "HomeAuto", "Homeauto", "SmartHome", "Smarthome", "DECT", "Dect", "Zigbee", "ULE", "HanFun"];

    private readonly ProbeSettings settings;
    private readonly ILoggerFactory loggerFactory;
    private readonly ILogger logger;
    private readonly Lazy<string> boxPassword;
    private readonly Lazy<string> brokerPassword;
    private readonly string runId = DateTime.Now.ToString("yyyyMMdd-HHmmss");
    private readonly Lock fileLock = new();
    private string? command;

    public Probe(ProbeSettings settings, ILoggerFactory loggerFactory)
    {
        this.settings = settings;
        this.loggerFactory = loggerFactory;
        logger = loggerFactory.CreateLogger<Probe>();
        boxPassword = new Lazy<string>(() => ReadSecret("FRITZBOX_PASSWORD", $"Password of box user '{settings.Box.UserName}'"));
        brokerPassword = new Lazy<string>(() => ReadSecret("MQTT_PASSWORD", $"Broker password of '{settings.Broker.UserName}'"));
    }

    private string AgentFile => Path.Combine(settings.OutputDirectory, "agent.json");

    private string RunDirectory => Path.Combine(settings.OutputDirectory, $"{runId}-{command}");

    public async Task InfoAsync(CancellationToken token)
    {
        command = "info";
        using var tr064 = await ConnectTr064Async(token).ConfigureAwait(false);
        var description = tr064.Description!;

        var report = new JsonObject
        {
            ["model"] = description.ModelName,
            ["friendlyName"] = description.FriendlyName,
            ["softwareVersion"] = description.SoftwareVersion,
            ["services"] = new JsonArray(description.Services.Select(JsonNode? (s) => s.ServiceType).ToArray()),
        };

        Console.WriteLine($"{description.ModelName} ({description.FriendlyName}), FRITZ!OS {description.SoftwareVersion}, {description.Services.Count} TR-064 services");

        foreach (var name in (string[])[UspControllerService, "X_AVM-DE_Auth", "X_AVM-DE_Homeauto", "ManagementServer"])
        {
            Console.WriteLine($"  {name}: {(description.FindService(name) != null ? "offered" : "MISSING")}");
        }

        if (description.FindService("X_AVM-DE_Auth") != null)
        {
            var auth = await tr064.InvokeAsync("X_AVM-DE_Auth", "GetInfo", [], token).ConfigureAwait(false);
            var enabled = Tr064Soap.ParseBoolean(auth.GetValueOrDefault("NewEnabled", "0"));
            report["secondFactorEnabled"] = enabled;
            Console.WriteLine($"  Confirmation on the box for protected actions: {(enabled ? "on" : "off")}");
        }

        if (description.FindService(UspControllerService) == null)
        {
            Console.WriteLine("This box has no TR-064 service for USP controllers.");
        }
        else
        {
            var info = await tr064.InvokeAsync(UspControllerService, "GetInfo", [], token).ConfigureAwait(false);
            report["uspControllerInfo"] = JsonSerializer.SerializeToNode(info);

            var actions = await tr064.GetActionsAsync(UspControllerService, token).ConfigureAwait(false);
            report["uspControllerActions"] = JsonSerializer.SerializeToNode(actions);
            Console.WriteLine($"  Actions of {UspControllerService} as the box declares them:");

            foreach (var (action, arguments) in actions)
            {
                Console.WriteLine($"    {action}({string.Join(", ", arguments.Where(a => !a.IsOutput).Select(a => a.Name))})");
            }

            var controllers = await new UspControllerRegistry(tr064).GetAllAsync(token).ConfigureAwait(false);
            report["uspControllers"] = JsonSerializer.SerializeToNode(controllers);
            PrintControllers(controllers);
        }

        Write("info.json", report);
    }

    public async Task RegisterAsync(CancellationToken token)
    {
        command = "register";
        using var tr064 = await ConnectTr064Async(token).ConfigureAwait(false);
        var registry = new UspControllerRegistry(tr064);
        var controller = settings.Controller;

        if (await registry.FindAsync(controller.EndpointId, token).ConfigureAwait(false) is { } existing)
        {
            Console.WriteLine($"{controller.EndpointId} is registered already.");
            PrintControllers([existing]);
            return;
        }

        var entry = new UspControllerEntry
        {
            EndpointId = controller.EndpointId,
            Mtp = "mqtt",
            Hostname = settings.Broker.Host,
            Port = settings.Broker.Port,
            UseTls = settings.Broker.UseTls,
            MqttControllerTopic = controller.MqttControllerTopic,
            MqttResponseTopic = controller.MqttResponseTopic,
            Username = controller.BoxUserName,
            AccessRights = controller.AccessRights
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Aggregate(UspAccessRights.None, (rights, name) => rights | Enum.Parse<UspAccessRights>(name, true)),
        };

        var password = ReadSecret("MQTT_BOX_PASSWORD", $"Broker password of the box's user '{controller.BoxUserName}'");

        var index = await registry.AddAsync
        (
            entry,
            password,
            (methods, _) =>
            {
                Console.WriteLine(ExplainConfirmation(methods));
                return Task.CompletedTask;
            },
            TimeSpan.FromMinutes(3),
            token
        ).ConfigureAwait(false);

        Console.WriteLine($"Registered as controller {index}.");
        PrintControllers(await registry.GetAllAsync(token).ConfigureAwait(false));
        File.Delete(AgentFile);
    }

    public async Task UnregisterAsync(CancellationToken token)
    {
        command = "unregister";
        using var tr064 = await ConnectTr064Async(token).ConfigureAwait(false);
        var registry = new UspControllerRegistry(tr064);

        if (await registry.FindAsync(settings.Controller.EndpointId, token).ConfigureAwait(false) is not { } existing)
        {
            Console.WriteLine($"{settings.Controller.EndpointId} is not registered.");
            return;
        }

        await registry.DeleteAsync(existing.Index, token).ConfigureAwait(false);
        Console.WriteLine($"Controller {existing.Index} ({existing.EndpointId}) removed.");
        File.Delete(AgentFile);
    }

    public async Task ListenAsync(int seconds, CancellationToken token)
    {
        command = "listen";
        await using var usp = await ConnectUspAsync(token).ConfigureAwait(false);
        Console.WriteLine($"Listening for {seconds} s ...");
        await Task.Delay(TimeSpan.FromSeconds(seconds), token).ConfigureAwait(false);
    }

    public async Task ExploreAsync(int waitSeconds, CancellationToken token)
    {
        command = "explore";
        await using var usp = await ConnectToBoxAsync(waitSeconds, token).ConfigureAwait(false);

        var protocol = await usp.GetSupportedProtocolAsync(token: token).ConfigureAwait(false);
        Console.WriteLine($"USP versions of the box: {protocol.AgentSupportedProtocolVersions}");
        Write("supported-protocol.json", protocol);

        Console.WriteLine("Reading the supported data model ...");
        var dataModel = await usp.GetSupportedDmAsync(["Device."], token: token).ConfigureAwait(false);
        Write("supported-dm.json", dataModel);

        Console.WriteLine("Reading the instances ...");
        Write("instances.json", await usp.GetInstancesAsync(["Device."], token: token).ConfigureAwait(false));

        Console.WriteLine("Reading all values ...");
        Write("values.json", await GetEverythingAsync(usp, dataModel, token).ConfigureAwait(false));

        var summary = Summarize(protocol, dataModel);
        Write("summary.md", summary);
        Console.WriteLine();
        Console.WriteLine(summary);
        Console.WriteLine($"Everything is in {Path.GetFullPath(RunDirectory)}");
    }

    public async Task GetAsync(IReadOnlyList<string> paths, CancellationToken token)
    {
        command = "get";
        await using var usp = await ConnectToBoxAsync(60, token).ConfigureAwait(false);
        var response = await usp.GetAsync(paths, token: token).ConfigureAwait(false);
        Console.WriteLine(ToJson(response).ToJsonString(indented));
        Write("get.json", response);
    }

    public async Task SetAsync(string path, string value, CancellationToken token)
    {
        command = "set";
        var lastDot = path.LastIndexOf('.');

        if (lastDot <= 0 || lastDot == path.Length - 1)
        {
            throw new ArgumentException($"{path} is no parameter path");
        }

        await using var usp = await ConnectToBoxAsync(60, token).ConfigureAwait(false);
        var response = await usp.SetAsync(path[..(lastDot + 1)], new Dictionary<string, string> { [path[(lastDot + 1)..]] = value }, token).ConfigureAwait(false);
        Console.WriteLine(ToJson(response).ToJsonString(indented));
        Write("set.json", response);
    }

    public async Task SubscribeAsync(int seconds, IReadOnlyList<string> paths, CancellationToken token)
    {
        command = "subscribe";
        await using var usp = await ConnectToBoxAsync(60, token).ConfigureAwait(false);

        usp.NotificationReceived += (_, e) =>
        {
            var notify = e.Notify;

            Console.WriteLine(notify.NotificationCase == Notify.NotificationOneofCase.ValueChange
                ? $"{e.Received.ToLocalTime():HH:mm:ss.fff} {notify.ValueChange.ParamPath} = {notify.ValueChange.ParamValue}"
                : $"{e.Received.ToLocalTime():HH:mm:ss.fff} {notify.NotificationCase}: {protobufJson.Format(notify)}");

            Append("notifications.jsonl", new JsonObject { ["received"] = e.Received, ["notify"] = ToJson(notify) });
        };

        var instance = await usp.SubscribeAsync($"usp-probe-{runId}", "ValueChange", paths, token: token).ConfigureAwait(false);
        Console.WriteLine($"Subscribed as {instance}. Waiting {seconds} s for notifications ...");

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds), token).ConfigureAwait(false);
        }
        finally
        {
            await usp.DeleteAsync([instance], CancellationToken.None).ConfigureAwait(false);
            Console.WriteLine($"{instance} deleted.");
        }
    }

    private async Task<Tr064Client> ConnectTr064Async(CancellationToken token)
    {
        var client = new Tr064Client
        (
            new Tr064ClientOptions
            {
                BaseUri = new Uri(settings.Box.Url),
                UserName = settings.Box.UserName,
                Password = boxPassword.Value,
                AcceptUntrustedCertificate = AcceptBoxCertificate,
            },
            loggerFactory.CreateLogger<Tr064Client>()
        );

        try
        {
            await client.ConnectAsync(token).ConfigureAwait(false);
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    /// <summary>The box's own certificate is self-signed. It is accepted on the LAN, and its fingerprint is logged.</summary>
    private bool AcceptBoxCertificate(X509Certificate2 certificate)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Accepting the box's certificate {Subject}, SHA-256 {Fingerprint}", certificate.Subject, certificate.GetCertHashString(HashAlgorithmName.SHA256));
        }

        return true;
    }

    private async Task<UspMqttController> ConnectUspAsync(CancellationToken token, JsonObject? agent = null)
    {
        var controller = settings.Controller;

        var usp = new UspMqttController
        (
            new UspMqttControllerOptions
            {
                BrokerHost = settings.Broker.Host,
                BrokerPort = settings.Broker.Port,
                UseTls = settings.Broker.UseTls,
                UserName = settings.Broker.UserName,
                Password = brokerPassword.Value,
                EndpointId = controller.EndpointId,
                ControllerTopic = controller.MqttControllerTopic,
                AgentEndpointId = agent?["endpointId"]?.GetValue<string>(),
                AgentTopic = agent?["topic"]?.GetValue<string>(),
                RequestTimeout = TimeSpan.FromMinutes(2),
                AdditionalTopicFilters = string.IsNullOrWhiteSpace(controller.ListenFilter) ? [] : [controller.ListenFilter],
            },
            loggerFactory.CreateLogger<UspMqttController>()
        );

        usp.RecordReceived += (_, e) =>
        {
            var record = e.Record;
            var message = e.Message;

            Console.WriteLine(record == null
                ? $"{e.Received.ToLocalTime():HH:mm:ss.fff} {e.Topic}: not a USP Record ({e.Error?.Message})"
                : $"{e.Received.ToLocalTime():HH:mm:ss.fff} {e.Topic}: {record.FromId} -> {record.ToId}, {record.RecordTypeCase}" +
                  (message == null ? string.Empty : $" {message.Header.MsgType} {message.Header.MsgId}"));

            Append("records.jsonl", new JsonObject
            {
                ["received"] = e.Received,
                ["topic"] = e.Topic,
                ["replyTo"] = e.ReplyTo,
                ["contentType"] = e.ContentType,
                ["error"] = e.Error?.Message,
                ["version"] = record?.Version,
                ["fromId"] = record?.FromId,
                ["toId"] = record?.ToId,
                ["recordType"] = record?.RecordTypeCase.ToString(),
                ["mqttConnect"] = record?.RecordTypeCase == UspRecord.Record.RecordTypeOneofCase.MqttConnect ? ToJson(record.MqttConnect) : null,
                ["disconnect"] = record?.RecordTypeCase == UspRecord.Record.RecordTypeOneofCase.Disconnect ? ToJson(record.Disconnect) : null,
                ["message"] = message == null ? null : ToJson(message),
            });
        };

        try
        {
            await usp.ConnectAsync(token).ConfigureAwait(false);
            return usp;
        }
        catch
        {
            await usp.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Connects to the broker and makes sure the box's Endpoint ID and topic are known: from probe output of an
    /// earlier run, or by switching our controller off and on again on the box, which makes it reconnect and send
    /// its MQTT Connect Record.
    /// </summary>
    private async Task<UspMqttController> ConnectToBoxAsync(int waitSeconds, CancellationToken token)
    {
        var agent = File.Exists(AgentFile) ? JsonNode.Parse(await File.ReadAllTextAsync(AgentFile, token).ConfigureAwait(false))?.AsObject() : null;
        var usp = await ConnectUspAsync(token, agent).ConfigureAwait(false);

        try
        {
            if (usp.AgentEndpointId == null || usp.AgentTopic == null)
            {
                await RestartControllerOnBoxAsync(token).ConfigureAwait(false);

                try
                {
                    await usp.WaitForAgentAsync(TimeSpan.FromSeconds(waitSeconds), token).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    throw new TimeoutException
                    (
                        $"The box did not introduce itself within {waitSeconds} s. Check with 'info' that the controller is registered and enabled, " +
                        $"in the broker log that '{settings.Controller.BoxUserName}' connects and subscribes, and with 'listen' what it publishes where."
                    );
                }

                Directory.CreateDirectory(settings.OutputDirectory);
                await File.WriteAllTextAsync(AgentFile, new JsonObject { ["endpointId"] = usp.AgentEndpointId, ["topic"] = usp.AgentTopic }.ToJsonString(indented), token).ConfigureAwait(false);
            }

            Console.WriteLine($"The box is {usp.AgentEndpointId} on {usp.AgentTopic}");
            return usp;
        }
        catch
        {
            await usp.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task RestartControllerOnBoxAsync(CancellationToken token)
    {
        using var tr064 = await ConnectTr064Async(token).ConfigureAwait(false);
        var registry = new UspControllerRegistry(tr064);

        var entry = await registry.FindAsync(settings.Controller.EndpointId, token).ConfigureAwait(false) ??
                    throw new InvalidOperationException($"{settings.Controller.EndpointId} is not registered on the box. Run 'register' first.");

        Console.WriteLine($"Switching controller {entry.Index} off and on, so that the box reconnects and introduces itself ...");
        await registry.SetEnabledAsync(entry.Index, false, token).ConfigureAwait(false);
        await Task.Delay(TimeSpan.FromSeconds(3), token).ConfigureAwait(false);
        await registry.SetEnabledAsync(entry.Index, true, token).ConfigureAwait(false);
    }

    /// <summary>All values below Device., at once, or object by object if the box will not answer that in one go.</summary>
    private async Task<JsonNode> GetEverythingAsync(UspMqttController usp, GetSupportedDMResp dataModel, CancellationToken token)
    {
        try
        {
            return ToJson(await usp.GetAsync(["Device."], token: token).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is UspException or TimeoutException)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Reading Device. at once failed ({Message}), reading object by object", ex.Message);
            }
        }

        var result = new JsonObject();

        var firstLevel = dataModel.ReqObjResults
            .SelectMany(r => r.SupportedObjs)
            .Select(o => o.SupportedObjPath)
            .Where(p => p.Count(c => c == '.') == 2)
            .Distinct();

        foreach (var path in firstLevel)
        {
            try
            {
                result[path] = ToJson(await usp.GetAsync([path], token: token).ConfigureAwait(false));
            }
            catch (Exception ex) when (ex is UspException or TimeoutException)
            {
                result[path] = ex.Message;
            }
        }

        return result;
    }

    private static string Summarize(GetSupportedProtocolResp protocol, GetSupportedDMResp dataModel)
    {
        var objects = dataModel.ReqObjResults.SelectMany(r => r.SupportedObjs).ToList();
        var parameters = objects.SelectMany(o => o.SupportedParams.Select(p => (Object: o, Parameter: p))).ToList();
        var text = new StringBuilder();

        text.AppendLine("# USP data model of the box");
        text.AppendLine();
        text.AppendLine($"- USP versions: {protocol.AgentSupportedProtocolVersions}");
        text.AppendLine($"- Data model: {string.Join(", ", dataModel.ReqObjResults.Select(r => r.DataModelInstUri).Distinct())}");
        text.AppendLine($"- {objects.Count} objects, {parameters.Count} parameters, {objects.Sum(o => o.SupportedCommands.Count)} commands, {objects.Sum(o => o.SupportedEvents.Count)} events");

        foreach (var group in parameters.GroupBy(p => p.Parameter.ValueChange))
        {
            text.AppendLine($"- {group.Key}: {group.Count()} parameters");
        }

        text.AppendLine();
        text.AppendLine("## Objects that look like smart home or AVM's own");
        text.AppendLine();

        foreach (var obj in objects.Where(o => interestingWords.Any(w => o.SupportedObjPath.Contains(w, StringComparison.Ordinal))))
        {
            text.AppendLine($"### {obj.SupportedObjPath} ({obj.Access}{(obj.IsMultiInstance ? ", multi-instance" : string.Empty)})");
            text.AppendLine();

            foreach (var parameter in obj.SupportedParams)
            {
                text.AppendLine($"- {parameter.ParamName}: {parameter.ValueType}, {parameter.Access}, {parameter.ValueChange}");
            }

            foreach (var commandResult in obj.SupportedCommands)
            {
                text.AppendLine($"- command {commandResult.CommandName}({string.Join(", ", commandResult.InputArgNames)})");
            }

            foreach (var eventResult in obj.SupportedEvents)
            {
                text.AppendLine($"- event {eventResult.EventName}({string.Join(", ", eventResult.ArgNames)})");
            }

            text.AppendLine();
        }

        text.AppendLine("## Writable parameters about IPv6");
        text.AppendLine();

        foreach (var (obj, parameter) in parameters.Where(p =>
                     (p.Object.SupportedObjPath + p.Parameter.ParamName).Contains("IPv6", StringComparison.OrdinalIgnoreCase) &&
                     p.Parameter.Access != GetSupportedDMResp.Types.ParamAccessType.ParamReadOnly))
        {
            text.AppendLine($"- {obj.SupportedObjPath}{parameter.ParamName}: {parameter.ValueType}, {parameter.Access}");
        }

        return text.ToString();
    }

    private static void PrintControllers(IEnumerable<UspControllerEntry> controllers)
    {
        foreach (var c in controllers)
        {
            Console.WriteLine
            (
                $"  [{c.Index}] {(c.Enable ? "on " : "off")} {c.EndpointId} {c.Mtp}://{c.Hostname}:{c.Port}{c.Path}{(c.UseTls ? " TLS" : string.Empty)} " +
                $"controller topic '{c.MqttControllerTopic}', response topic '{c.MqttResponseTopic}', user '{c.Username}', rights {c.AccessRights}"
            );
        }
    }

    private static string ExplainConfirmation(string methods) => string.Join
    (
        Environment.NewLine,
        methods.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(method => method switch
        {
            "button" => ">>> Press any button on the box now.",
            _ when method.StartsWith("dtmf;", StringComparison.Ordinal) => $">>> Or dial {method[5..]} on a phone connected to the box.",
            _ => $">>> Confirm on the box: {method}",
        })
    );

    private static string ReadSecret(string variable, string prompt)
    {
        var value = Environment.GetEnvironmentVariable(variable);

        if (!string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (Console.IsInputRedirected)
        {
            throw new InvalidOperationException($"Set the environment variable {variable}");
        }

        Console.Write($"{prompt}: ");
        var text = new StringBuilder();

        while (Console.ReadKey(intercept: true) is var key && key.Key != ConsoleKey.Enter)
        {
            if (key.Key == ConsoleKey.Backspace)
            {
                text.Length = Math.Max(0, text.Length - 1);
            }
            else if (!char.IsControl(key.KeyChar))
            {
                text.Append(key.KeyChar);
            }
        }

        Console.WriteLine();
        return text.ToString();
    }

    private static JsonNode ToJson(IMessage message) => JsonNode.Parse(protobufJson.Format(message))!;

    private void Write(string fileName, IMessage message) => Write(fileName, ToJson(message));

    private void Write(string fileName, JsonNode node) => Write(fileName, node.ToJsonString(indented));

    private void Write(string fileName, string text)
    {
        Directory.CreateDirectory(RunDirectory);
        File.WriteAllText(Path.Combine(RunDirectory, fileName), text);
    }

    private void Append(string fileName, JsonNode node)
    {
        lock (fileLock)
        {
            Directory.CreateDirectory(RunDirectory);
            File.AppendAllText(Path.Combine(RunDirectory, fileName), node.ToJsonString() + Environment.NewLine);
        }
    }
}
