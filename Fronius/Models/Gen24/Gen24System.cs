﻿using CommunityToolkit.Mvvm.ComponentModel;
using DocumentFormat.OpenXml;

namespace De.Hochstaetter.Fronius.Models.Gen24;

public partial class Gen24System : BindableBase, IHaveDisplayName, IHaveUniqueId
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(DisplayName), nameof(Model), nameof(SerialNumber))]
    public partial Gen24Config? Config { get; set; }

    [ObservableProperty]
    public partial Gen24Sensors? Sensors { get; set; }

    [JsonIgnore] public IGen24Service Service { get; init; } = null!;

    [JsonIgnore] public string DisplayName => $"{Config?.Versions?.ModelName ?? "GEN24"} - {Config?.InverterSettings?.SystemName ?? "GEN24"} - {Config?.Versions?.SerialNumber ?? "---"}";

    public override string ToString() => DisplayName;

    public bool IsPresent => true;
    public string? Manufacturer => "Fronius";
    public string? Model => Config?.Versions?.ModelName;
    public string? SerialNumber => Config?.Versions?.SerialNumber;

    public void CopyFrom(Gen24System other)
    {
        ArgumentNullException.ThrowIfNull(other, nameof(other));
        Config = other.Config;
        Sensors = other.Sensors;
    }
}
