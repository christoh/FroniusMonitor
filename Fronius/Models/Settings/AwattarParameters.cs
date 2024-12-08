namespace De.Hochstaetter.Fronius.Models.Settings;

[XmlType("Awattar")]
public class AwattarParameters : BindableBase
{
    private string bearer = string.Empty;

    [XmlAttribute]
    public string Bearer
    {
        get => bearer;
        set => Set(ref bearer, value);
    }

    private string postalCode = "85375";

    [XmlAttribute]
    public string PostalCode
    {
        get => postalCode;
        set => Set(ref postalCode, value);
    }

    private string gridOperatorId = "9901068000001";

    [XmlAttribute]
    public string GridOperatorId
    {
        get => gridOperatorId;
        set => Set(ref gridOperatorId, value);
    }
}
