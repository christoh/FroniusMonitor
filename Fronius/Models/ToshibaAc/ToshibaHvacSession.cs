namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public partial class ToshibaHvacSession : BindableBase
{
    [ObservableProperty, JsonPropertyName("consumerId"), JsonRequired]
    public partial Guid ConsumerId { get; set; }

    [ObservableProperty, JsonPropertyName("access_token"), JsonRequired]
    public partial string AccessToken { get; set; } = string.Empty;

    [ObservableProperty, JsonPropertyName("token_type")]
    public partial string TokenType { get; set; } = string.Empty;

    [ObservableProperty, JsonPropertyName("consumerMasterId")]
    public partial string ConsumerMasterId { get; set; } = string.Empty;

    [ObservableProperty, JsonPropertyName("countryId")]
    public partial int CountryId { get; set; }
}