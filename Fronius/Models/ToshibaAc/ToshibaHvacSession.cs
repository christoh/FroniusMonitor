namespace De.Hochstaetter.Fronius.Models.ToshibaAc
{
    public class ToshibaHvacSession : BindableBase
    {
        private Guid consumerId;

        [JsonPropertyName("consumerId")]
        [JsonRequired]
        public Guid ConsumerId
        {
            get => consumerId;
            set => Set(ref consumerId, value);
        }

        private string accessToken = string.Empty;

        [JsonPropertyName("access_token")]
        [JsonRequired]
        public string AccessToken
        {
            get => accessToken;
            set => Set(ref accessToken, value);
        }

        private string tokenType = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType
        {
            get => tokenType;
            set => Set(ref tokenType, value);
        }

        private string consumerMasterId = string.Empty;

        [JsonPropertyName("consumerMasterId")]
        public string ConsumerMasterId
        {
            get => consumerMasterId;
            set => Set(ref consumerMasterId, value);
        }

        private int countryId;
        [JsonPropertyName("countryId")]
        public int CountryId
        {
            get => countryId;
            set => Set(ref countryId, value);
        }
    }
}
