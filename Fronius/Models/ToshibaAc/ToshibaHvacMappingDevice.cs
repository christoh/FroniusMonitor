namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public partial class ToshibaHvacMappingDevice : ToshibaHvacDeviceBase
{
    [ObservableProperty, JsonPropertyName("Name"), NotifyPropertyChangedFor(nameof(DisplayName))]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty, JsonPropertyName("Id")]
    public partial Guid AcId { get; set; }

    [JsonPropertyName("DeviceUniqueId"), ObservableProperty, NotifyPropertyChangedFor(nameof(SerialNumber))]
    public override partial Guid DeviceUniqueId { get; set; }

    [ObservableProperty, JsonPropertyName("ACModelId")]
    public partial int AcModelId { get; set; }

    [ObservableProperty, JsonPropertyName("Description")]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty, JsonPropertyName("CreatedDate")]
    public partial string CreatedDate { get; set; } = string.Empty;

    public override string DisplayName => Name;

    /// <summary>See <see cref="ToshibaHvacDeviceBase.CopyFrom" />. The unique id is the identity and is not copied.</summary>
    public void CopyFrom(ToshibaHvacMappingDevice other)
    {
        base.CopyFrom(other);
        Name = other.Name;
        AcId = other.AcId;
        AcModelId = other.AcModelId;
        Description = other.Description;
        CreatedDate = other.CreatedDate;
    }

    public override string ToString() => $"{Name} ({Description})";
}
