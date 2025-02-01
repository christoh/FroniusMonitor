namespace De.Hochstaetter.Fronius.Models.WebApi;

public class WebApiInfo : BindableBase
{
    public string? ProductName
    {
        get;
        set => Set(ref field, value);
    }

    public Version? Version
    {
        get;
        set => Set(ref field, value);
    }

    public string? Manufacturer
    {
        get;
        set => Set(ref field, value);
    }

    public string? OsName
    {
        get;
        set => Set(ref field, value);
    }

    public string? OsVersion
    {
        get;
        set => Set(ref field, value);
    }
}