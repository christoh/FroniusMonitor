namespace De.Hochstaetter.Fronius.Models;

public partial class BaseResponse : BindableBase
{
    [ObservableProperty]
    public partial int StatusCode { get; set; }

    [ObservableProperty]
    public partial string Reason { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string UserMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime Timestamp { get; set; } = DateTime.MinValue;

    public virtual string DisplayName => string.Empty;
    public override string ToString() => DisplayName;
}