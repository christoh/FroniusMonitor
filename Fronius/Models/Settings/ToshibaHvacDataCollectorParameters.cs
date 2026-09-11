namespace De.Hochstaetter.Fronius.Models.Settings;

/// <summary>
///     What <see cref="Services.DataCollectors.ToshibaHvacDataCollector" /> is configured with: the one Toshiba
///     account, the id this installation registers under, and how often the device list is read again.
/// </summary>
public class ToshibaHvacDataCollectorParameters
{
    /// <summary>The account; <see langword="null" /> or an empty user name means no Toshiba devices are collected.</summary>
    public AzureConnection? Connection { get; set; }

    /// <summary>The six digit id from the settings - see <see cref="ToshibaHvacAzureDeviceId" />.</summary>
    public string AzureDeviceId { get; set; } = string.Empty;

    /// <summary>
    ///     How often <c>GetConsumerACMapping</c> is read again. Message queuing loses the odd message, so the full
    ///     state comes from the HTTPS side at this rate regardless of what the IoT Hub delivered.
    /// </summary>
    public TimeSpan MappingRefreshRate { get; set; } = TimeSpan.FromMinutes(30);
}
