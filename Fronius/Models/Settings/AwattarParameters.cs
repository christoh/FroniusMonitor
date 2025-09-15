namespace De.Hochstaetter.Fronius.Models.Settings;

[XmlType("Awattar")]
public partial class AwattarParameters : BindableBase
{
    [ObservableProperty]
    [XmlAttribute]
    public partial string Bearer { get; set; } = string.Empty;

    [ObservableProperty]
    [XmlAttribute]
    public partial string PostalCode { get; set; } = "85375"; // Neufahrn bei Freising

    [ObservableProperty]
    [XmlAttribute]
    public partial string GridOperatorId { get; set; } = "9901068000001"; // Bayernwerk (E.ON Bayern)
}
