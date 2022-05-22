namespace De.Hochstaetter.Fronius.Models;

public class WebConnection : BindableBase
{
    private string? baseUrl;
    [DefaultValue(null), XmlAttribute]
    public string? BaseUrl
    {
        get => baseUrl;
        set => Set(ref baseUrl, value);
    }

    private string? userName;
    [DefaultValue(null), XmlAttribute]
    public string? UserName
    {
        get => userName;
        set => Set(ref userName, value);
    }

    private string? password;
    [DefaultValue(null), XmlAttribute]
    public string? Password
    {
        get => password;
        set => Set(ref password, value);
    }
}