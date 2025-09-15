namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public partial class ToshibaHvacResponse<T> : BindableBase where T : new()
{
    [ObservableProperty, JsonPropertyName("IsSuccess"), JsonRequired]
    public partial bool IsSuccess { get; set; }

    [ObservableProperty, JsonPropertyName("ResObj"), JsonRequired]
    public partial T Data { get; set; } = new();

    [ObservableProperty, JsonPropertyName("Message"), JsonRequired]
    public partial string Message { get; set; } = string.Empty;
}
