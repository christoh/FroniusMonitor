namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

/// <summary>
///     The six digit number this installation registers itself under with the Toshiba service, as the "mobile
///     device" <c>&lt;user name&gt;_&lt;id&gt;</c>. It is drawn once at random, written to the settings and never
///     changed afterwards, so the service keeps seeing the same device.
/// </summary>
public static class ToshibaHvacAzureDeviceId
{
    private const string Format = "D6";

    public static uint CreateRandom() => unchecked((uint)RandomNumberGenerator.GetInt32(0, 1000000));

    public static string ToString(uint id) => id.ToString(Format, CultureInfo.InvariantCulture);

    /// <summary>
    ///     The id from its settings text, or a fresh random one when the text is not a number - which is logged,
    ///     because a changed id makes the Toshiba service see a new device.
    /// </summary>
    public static uint Parse(string? text, ILogger? logger)
    {
        if (uint.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        result = CreateRandom();

        if (logger != null && logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning("Invalid AzureDeviceId value: {Value}. Generated a new random value: {RandomValue}", text, ToString(result));
        }

        return result;
    }
}
