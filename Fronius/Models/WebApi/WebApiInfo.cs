namespace De.Hochstaetter.Fronius.Models.WebApi;

public partial class WebApiInfo : BindableBase
{
    [ObservableProperty]
    public partial string? ProductName { get; set; }

    [ObservableProperty]
    public partial string? Version { get; set; }

    [ObservableProperty]
    public partial string? Manufacturer { get; set; }

    [ObservableProperty]
    public partial string? OsArch { get; set; }

    [ObservableProperty]
    public partial string? OsVersion { get; set; }

    [ObservableProperty]
    public partial string? DotNetVersion { get; set; }
}