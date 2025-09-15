namespace De.Hochstaetter.Fronius.Models.WebApi;

public partial class WebApiInfo : BindableBase
{
    [ObservableProperty]
    public partial string? ProductName { get; set; }

    [ObservableProperty]
    public partial Version? Version { get; set; }

    [ObservableProperty]
    public partial string? Manufacturer { get; set; }

    [ObservableProperty]
    public partial string? OsName { get; set; }

    [ObservableProperty]
    public partial string? OsVersion { get; set; }
}