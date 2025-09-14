using De.Hochstaetter.Fronius.JsonConverters;

namespace De.Hochstaetter.Fronius.Models;

public partial class AwattarPriceInfo : BindableBase
{
    [ObservableProperty]
    public partial string? Object { get; set; }

    [ObservableProperty]
    public partial AwattarData Data { get; set; } = new();
}

public partial class AwattarData : BindableBase
{
    [ObservableProperty]
    public partial List<AwattarPriceComponent> Prices { get; set; } = [];
}

public partial class AwattarPriceComponent : BindableBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GrossPrice))]
    [JsonConverter(typeof(AwattarPriceInfoConverter))]
    public partial decimal Price { get; set; }

    public decimal GrossPrice => Price + Price * TaxRate;

    [ObservableProperty]
    public partial string? Description { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GrossPrice))]
    public partial decimal TaxRate { get; set; }

    [ObservableProperty]
    public partial string? Name { get; set; }

    [ObservableProperty]
    public partial string? PriceUnit { get; set; }
}