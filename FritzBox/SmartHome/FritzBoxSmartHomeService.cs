using System.Xml.Serialization;
using De.Hochstaetter.FritzBox.Http;
using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Settings;

namespace De.Hochstaetter.FritzBox.SmartHome;

/// <summary>
/// The smart home of a box over the Smart Home REST API where the box has it (FRITZ!OS 8.20 and later) and over AHA
/// otherwise. Which of the two is decided once per connection, from the FRITZ!OS version. Either way the callers get
/// the same <see cref="FritzBoxDevice"/>s and address them by the same AIN.
/// </summary>
public sealed class FritzBoxSmartHomeService : IFritzBoxService, IDisposable
{
    private static readonly Version restApiVersion = new(8, 20);
    private static readonly XmlSerializer ahaSerializer = new(typeof(FritzBoxDeviceList));

    private readonly ILogger logger;
    private readonly Func<FritzBoxHttpClientOptions, FritzBoxHttpClient> createClient;
    private readonly Lock gate = new();
    private Box? box;

    public FritzBoxSmartHomeService(ILoggerFactory loggerFactory) : this(loggerFactory, options => new FritzBoxHttpClient(options, loggerFactory.CreateLogger<FritzBoxHttpClient>())) { }

    internal FritzBoxSmartHomeService(ILoggerFactory loggerFactory, Func<FritzBoxHttpClientOptions, FritzBoxHttpClient> createClient)
    {
        logger = loggerFactory.CreateLogger<FritzBoxSmartHomeService>();
        this.createClient = createClient;
    }

    public event EventHandler? CommandSent;

    /// <summary>The box. A change of its address, user or password takes effect with the next call.</summary>
    public WebConnection? Connection { get; set; }

    public async ValueTask FritzBoxLogin(CancellationToken token = default) => await GetBox().Http.LogInAsync(token).ConfigureAwait(false);

    public async Task<FritzBoxDeviceList> GetDevices(CancellationToken token = default)
    {
        var current = GetBox();
        FritzBoxDeviceList result;

        if (await UsesRestApiAsync(current, token).ConfigureAwait(false))
        {
            var overview = await current.SmartHome.GetOverviewAsync(token).ConfigureAwait(false);
            current.Remember(overview);
            result = SmartHomeMapper.ToDeviceList(overview);
            result.FirmwareVersionString = current.FritzOsVersion is { } version ? $"{version.Major}.{version.Minor:00}" : null;
        }
        else
        {
            using var reader = new StringReader(await current.Http.GetAhaAsync("switchcmd=getdevicelistinfos", token).ConfigureAwait(false));
            result = ahaSerializer.Deserialize(reader) as FritzBoxDeviceList ?? throw new InvalidDataException("getdevicelistinfos answered with an empty device list");
        }

        foreach (var device in result.Devices)
        {
            device.FritzBoxService = this;
        }

        return result;
    }

    public ValueTask SwitchDevice(string ain, bool turnOn, CancellationToken token = default) => SendAsync
    (
        ain,
        new SmartHomeInterfaces { OnOffInterface = new SmartHomeOnOff { Active = turnOn } },
        $"switchcmd=setswitcho{(turnOn ? "n" : "ff")}",
        token
    );

    /// <summary>Sets the brightness, 0..1. The lowest level sent is the box's lowest above dark (1 %, 2/255 over AHA).</summary>
    public ValueTask SetLevel(string ain, double level, CancellationToken token = default) => SendAsync
    (
        ain,
        new SmartHomeInterfaces { LevelControlInterface = new SmartHomeLevelControl { Level = ToRange(level * 100, 1, 100) } },
        $"switchcmd=setlevel&level={ToRange(level * 255, 2, 255)}",
        token
    );

    public ValueTask SetColorTemperature(string ain, double temperatureKelvin, CancellationToken token = default) => SendAsync
    (
        ain,
        new SmartHomeInterfaces { ColorControlInterface = new SmartHomeColorControl { ColorTemperature = ToRange(temperatureKelvin, 0, int.MaxValue) } },
        $"switchcmd=setcolortemperature&temperature={ToRange(temperatureKelvin, 0, int.MaxValue)}&duration=0",
        token
    );

    /// <summary>Sets hue (degrees) and saturation (0..1, where 1 is <see cref="FritzBoxColorControl.MaxSaturation"/>).</summary>
    public ValueTask SetColor(string ain, double hueDegrees, double saturation, CancellationToken token = default)
    {
        var hue = ToRange(hueDegrees, 0, 359);
        var saturationAbsolute = ToRange(saturation * FritzBoxColorControl.MaxSaturation, 0, FritzBoxColorControl.MaxSaturation);

        return SendAsync
        (
            ain,
            new SmartHomeInterfaces { ColorControlInterface = new SmartHomeColorControl { HsColor = new SmartHomeHsColor { Hue = hue, Saturation = saturationAbsolute } } },
            $"switchcmd=setunmappedcolor&hue={hue}&saturation={saturationAbsolute}&duration=0",
            token
        );
    }

    public void Dispose()
    {
        lock (gate)
        {
            box?.Dispose();
            box = null;
        }
    }

    private async ValueTask SendAsync(string ain, SmartHomeInterfaces restCommand, string ahaCommand, CancellationToken token)
    {
        var current = GetBox();

        if (await UsesRestApiAsync(current, token).ConfigureAwait(false))
        {
            await current.SmartHome.SetUnitAsync(await current.GetUnitUidAsync(ain, token).ConfigureAwait(false), restCommand, token).ConfigureAwait(false);
        }
        else
        {
            await current.Http.GetAhaAsync($"ain={Uri.EscapeDataString(Box.Compact(ain))}&{ahaCommand}", token).ConfigureAwait(false);
        }

        CommandSent?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Whether the box has the REST API, from its FRITZ!OS version. A version that was read is remembered for the
    /// connection. One that could not be read (an error page, a box busy restarting) is not: that call goes over AHA,
    /// which every box has, and the next one asks again.
    /// </summary>
    private async ValueTask<bool> UsesRestApiAsync(Box current, CancellationToken token)
    {
        if (current.UsesRestApi is { } known)
        {
            return known;
        }

        if (await current.Http.GetFritzOsVersionAsync(token).ConfigureAwait(false) is not { } version)
        {
            if (!current.ReportedUnknownVersion && logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("FRITZ!Box at {BaseUri} does not tell its FRITZ!OS version: using the AHA interface until it does", current.BaseUri);
            }

            current.ReportedUnknownVersion = true;
            return false;
        }

        var usesRestApi = version >= restApiVersion;

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("FRITZ!Box at {BaseUri} runs FRITZ!OS {Version}: using the {Interface}", current.BaseUri, version, usesRestApi ? "Smart Home REST API" : "AHA interface");
        }

        current.FritzOsVersion = version;
        current.UsesRestApi = usesRestApi;
        return usesRestApi;
    }

    /// <summary>The client for the current <see cref="Connection"/>, made anew when the connection has changed.</summary>
    private Box GetBox()
    {
        var connection = Connection ?? throw new InvalidOperationException("No FRITZ!Box connection is set");
        var key = (connection.BaseUrl, connection.UserName, connection.Password, connection.CertificateFingerprint);

        if (!Uri.TryCreate(connection.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri) || baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException($"'{connection.BaseUrl}' is not the http or https address of a FRITZ!Box");
        }

        lock (gate)
        {
            if (box is { } current && current.Key == key)
            {
                return current;
            }

            box?.Dispose();

            var http = createClient(new FritzBoxHttpClientOptions
            {
                BaseUri = baseUri,
                UserName = connection.UserName,
                Password = connection.Password,
                AcceptUntrustedCertificate = new UntrustedCertificatePolicy(baseUri, connection.CertificateFingerprint, logger).Accept,
            });

            return box = new Box(key, http);
        }
    }

    /// <summary>Rounds and clamps <paramref name="value"/>. A NaN, which the web API's double parameters let through, becomes the minimum.</summary>
    private static int ToRange(double value, int minimum, int maximum) => double.IsNaN(value) ? minimum : (int)Math.Clamp(Math.Round(value, MidpointRounding.AwayFromZero), minimum, maximum);

    /// <summary>One connection to one box, with what was learned about it.</summary>
    private sealed class Box((string, string, string, string?) key, FritzBoxHttpClient http) : IDisposable
    {
        /// <summary>The unit UIDs of the REST API by their AIN without spaces, as callers may write it either way.</summary>
        private readonly ConcurrentDictionary<string, string> unitUids = new();

        public (string, string, string, string?) Key => key;
        public FritzBoxHttpClient Http => http;
        public SmartHomeClient SmartHome { get; } = new(http);
        public Uri BaseUri { get; } = http.BaseUri;
        public bool? UsesRestApi { get; set; }
        public Version? FritzOsVersion { get; set; }
        public bool ReportedUnknownVersion { get; set; }

        public static string Compact(string ain) => ain.Replace(" ", string.Empty, StringComparison.Ordinal);

        public void Remember(SmartHomeOverview overview)
        {
            foreach (var unit in overview.Units)
            {
                unitUids[Compact(string.IsNullOrEmpty(unit.Ain) ? unit.Uid : unit.Ain)] = unit.Uid;
            }
        }

        /// <summary>
        /// The UID that belongs to <paramref name="ain"/>. They are the same for every unit seen so far, but the API
        /// keeps them apart, so the overview decides. A unit it does not know is addressed by the AIN as it is.
        /// </summary>
        public async Task<string> GetUnitUidAsync(string ain, CancellationToken token)
        {
            if (unitUids.TryGetValue(Compact(ain), out var uid))
            {
                return uid;
            }

            Remember(await SmartHome.GetOverviewAsync(token).ConfigureAwait(false));
            return unitUids.GetValueOrDefault(Compact(ain), ain);
        }

        public void Dispose() => http.Dispose();
    }
}
