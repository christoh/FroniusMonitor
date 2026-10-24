using De.Hochstaetter.FritzBoxUspProbe;

const string usage = """
    FritzBoxUspProbe [--config probe.json] [--verbose] <command>

      info                           model, FRITZ!OS, TR-064 services and the box's USP controllers
      register                       adds the controller of probe.json to the box
      unregister                     removes it again
      listen <seconds>               prints every USP Record on the broker for a while
      explore [<seconds>]            waits for the box (default 120 s), then reads its whole data model
      get <path>...                  for example: get Device.DeviceInfo.
      set <path> <value>             for example: set Device.SomeObject.1.Enable true
      subscribe [--type <type>] <seconds> <path>...
                                     subscriptions, one per path, prints the notifications, deletes them afterwards;
                                     type ValueChange (default), ObjectCreation, ObjectDeletion, Event
      delete <path>...               deletes objects, for example: delete Device.LocalAgent.Subscription.1.
      aha <query>                    the AHA answer, for example: aha switchcmd=getdevicelistinfos
      rest <path>                    the REST answer, for example: rest smarthome/overview
      smarthome [<ain> <command>]    the devices as the HA server gets them, REST or AHA by FRITZ!OS version;
                                     with an AIN, first: on, off, level <percent>, kelvin <K>, color <hue> <percent>
      routine <uid> on|off           switches a routine of the box on or off (REST API)

    Passwords come from FRITZBOX_PASSWORD, MQTT_PASSWORD and MQTT_BOX_PASSWORD, or are asked for.
    """;

var arguments = args.ToList();
var configPath = TakeOption(arguments, "--config") ?? "probe.json";
var verbose = arguments.Remove("--verbose");
var notificationType = TakeOption(arguments, "--type") ?? "ValueChange";

if (arguments.Count == 0)
{
    Console.WriteLine(usage);
    return 2;
}

using var loggerFactory = LoggerFactory.Create(builder => builder
    .AddSimpleConsole(o => o.TimestampFormat = "HH:mm:ss ")
    .SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information));

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellation.Cancel();
};

try
{
    var probe = new Probe(ProbeSettings.Load(configPath), loggerFactory);
    var command = arguments[0];
    var rest = arguments.Skip(1).ToList();

    await (command switch
    {
        "info" => probe.InfoAsync(cancellation.Token),
        "register" => probe.RegisterAsync(cancellation.Token),
        "unregister" => probe.UnregisterAsync(cancellation.Token),
        "listen" when rest.Count == 1 => probe.ListenAsync(int.Parse(rest[0]), cancellation.Token),
        "explore" => probe.ExploreAsync(rest.Count > 0 ? int.Parse(rest[0]) : 120, cancellation.Token),
        "get" when rest.Count > 0 => probe.GetAsync(rest, cancellation.Token),
        "set" when rest.Count == 2 => probe.SetAsync(rest[0], rest[1], cancellation.Token),
        "subscribe" when rest.Count > 1 => probe.SubscribeAsync(notificationType, int.Parse(rest[0]), rest.Skip(1).ToList(), cancellation.Token),
        "delete" when rest.Count > 0 => probe.DeleteAsync(rest, cancellation.Token),
        "aha" when rest.Count == 1 => probe.AhaAsync(rest[0], cancellation.Token),
        "rest" when rest.Count == 1 => probe.RestAsync(rest[0], cancellation.Token),
        "smarthome" => probe.SmartHomeAsync(rest, cancellation.Token),
        "routine" when rest is [_, "on" or "off"] => probe.RoutineAsync(rest[0], rest[1] == "on", cancellation.Token),
        _ => throw new ArgumentException($"Unknown command or wrong arguments: {string.Join(' ', arguments)}{Environment.NewLine}{usage}"),
    });

    return 0;
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Console.WriteLine("Cancelled.");
    return 1;
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(verbose ? ex.ToString() : $"{ex.GetType().Name}: {ex.Message}");
    Console.ResetColor();
    return 1;
}

static string? TakeOption(List<string> arguments, string name)
{
    var index = arguments.IndexOf(name);

    if (index < 0 || index == arguments.Count - 1)
    {
        return null;
    }

    var value = arguments[index + 1];
    arguments.RemoveRange(index, 2);
    return value;
}
