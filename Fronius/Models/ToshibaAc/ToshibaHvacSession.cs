namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public class ToshibaHvacSession : BindableBase
{
    [JsonPropertyName("consumerId")]
    [JsonRequired]
    public Guid ConsumerId
    {
        get;
        set => Set(ref field, value);
    }

    [JsonPropertyName("access_token")]
    [JsonRequired]
    public string AccessToken
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [JsonPropertyName("consumerMasterId")]
    public string ConsumerMasterId
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [JsonPropertyName("countryId")]
    public int CountryId
    {
        get;
        set => Set(ref field, value);
    }
}