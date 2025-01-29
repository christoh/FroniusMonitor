namespace De.Hochstaetter.Fronius.Models.Settings;

[XmlType("Awattar")]
public class AwattarParameters : BindableBase
{
    [XmlAttribute]
    public string Bearer
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [XmlAttribute]
    public string PostalCode
    {
        get;
        set => Set(ref field, value);
    } = "85375";

    [XmlAttribute]
    public string GridOperatorId
    {
        get;
        set => Set(ref field, value);
    } = "9901068000001";
}
