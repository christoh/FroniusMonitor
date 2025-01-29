namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public class ToshibaHvacResponse<T> : BindableBase where T : new()
{
    [JsonPropertyName("IsSuccess")]
    [JsonRequired]
    public bool IsSuccess
    {
        get;
        set => Set(ref field, value);
    }

    [JsonPropertyName("ResObj")]
    [JsonRequired]
    public T Data
    {
        get;
        set => Set(ref field, value);
    } = new();

    [JsonPropertyName("Message")]
    [JsonRequired]
    public string Message
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;
}
