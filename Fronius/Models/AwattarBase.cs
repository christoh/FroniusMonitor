namespace De.Hochstaetter.Fronius.Models;

public abstract partial class AwattarBase : BindableBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StartTime))]
    [JsonPropertyName("start_timestamp")]
    public partial long StartMilliseconds { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EndTime))]
    [JsonPropertyName("end_timestamp")]
    public partial long EndMilliseconds { get; set; }

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
