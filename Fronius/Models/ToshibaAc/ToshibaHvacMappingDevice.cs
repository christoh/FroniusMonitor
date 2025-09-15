namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public partial class ToshibaHvacMappingDevice : ToshibaHvacDeviceBase
{
    [ObservableProperty, JsonPropertyName("Name")]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty, JsonPropertyName("Id")]
    public partial Guid AcId { get; set; } 

    [JsonPropertyName("DeviceUniqueId"), ObservableProperty, NotifyPropertyChangedFor(nameof(SerialNumber))]
    public override partial Guid DeviceUniqueId { get; set; } 

    [ObservableProperty, JsonPropertyName("ACModelId")]
    public partial int AcModelId { get; set; } 

    [ObservableProperty, JsonPropertyName("Description")]
    public partial string Description { get; set; }  = string.Empty;

    [ObservableProperty, JsonPropertyName("CreatedDate")]
    public partial string CreatedDate { get; set; }  = string.Empty;

    public override string ToString() => $"{Name} ({Description})";
}