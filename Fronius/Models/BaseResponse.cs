namespace De.Hochstaetter.Fronius.Models;

public class BaseResponse : BindableBase
{
    public int StatusCode
    {
        get;
        set => Set(ref field, value);
    }

    public string Reason
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    public string UserMessage
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    public DateTime Timestamp
    {
        get;
        set => Set(ref field, value);
    } = DateTime.MinValue;

    public virtual string DisplayName => string.Empty;
    public override string ToString() => DisplayName;
}