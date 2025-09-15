namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public partial class ToshibaHvacMapping : BindableBase
{
    [ObservableProperty, JsonRequired, JsonPropertyName("GroupId")]
    public partial Guid GroupId { get; set; }

    [ObservableProperty, JsonPropertyName("GroupName")]
    public partial string GroupName { get; set; } = string.Empty;

    [ObservableProperty, JsonPropertyName("ConsumerId")]
    public partial Guid ConsumerId { get; set; }

    [ObservableProperty, JsonPropertyName("TimeZone")]
    public partial string TimeZone { get; set; } = string.Empty;

    [ObservableProperty, JsonPropertyName("ACList")]
    public partial ObservableCollection<ToshibaHvacMappingDevice> Devices { get; set; } = [];

    public override string ToString() => GroupName;
}