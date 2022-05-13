namespace De.Hochstaetter.Fronius.Models
{
    public class WebConnection : BindableBase
    {
        [DefaultValue(null), XmlAttribute] public string? BaseUrl { get; init; }
        [DefaultValue(null), XmlAttribute] public string? UserName { get; init; }
        [DefaultValue(null), XmlAttribute] public string? Password { get; init; }
    }
}
