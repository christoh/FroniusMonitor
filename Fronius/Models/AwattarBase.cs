namespace De.Hochstaetter.Fronius.Models;

public abstract class AwattarBase : BindableBase
{
    [JsonPropertyName("start_timestamp")]
    public long StartMilliseconds
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(StartTime)));
    }

    [JsonPropertyName("end_timestamp")]
    public long EndMilliseconds
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(EndTime)));
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
