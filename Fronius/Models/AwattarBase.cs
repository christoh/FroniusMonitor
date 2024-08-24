namespace De.Hochstaetter.Fronius.Models;

public abstract class AwattarBase : BindableBase
{
    private long startMilliseconds;

    [JsonPropertyName("start_timestamp")]
    public long StartMilliseconds
    {
        get => startMilliseconds;
        set => Set(ref startMilliseconds, value, () => NotifyOfPropertyChange(nameof(StartTime)));
    }

    private long endMilliseconds;

    [JsonPropertyName("end_timestamp")]
    public long EndMilliseconds
    {
        get => endMilliseconds;
        set => Set(ref endMilliseconds, value, () => NotifyOfPropertyChange(nameof(EndTime)));
    }

    [JsonIgnore]
    public DateTime StartTime
    {
        get => DateTime.UnixEpoch.AddMilliseconds(StartMilliseconds);
        set => StartMilliseconds = (long)Math.Round((value.ToUniversalTime() - DateTime.UnixEpoch).TotalMilliseconds, MidpointRounding.AwayFromZero);
    }

    [JsonIgnore]
    public DateTime EndTime
    {
        get => DateTime.UnixEpoch.AddMilliseconds(EndMilliseconds);
        set => EndMilliseconds = (long)Math.Round((value.ToUniversalTime() - DateTime.UnixEpoch).TotalMilliseconds, MidpointRounding.AwayFromZero);
    }
}
