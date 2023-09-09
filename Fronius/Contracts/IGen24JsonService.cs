namespace De.Hochstaetter.Fronius.Contracts
{
    public interface IGen24JsonService
    {
        T ReadFroniusData<T>(JToken? device) where T : new();
        object? ReadEnum(Type type, string? stringValue);
        JObject GetUpdateToken<T>(T newEntity, T? oldEntity = default) where T : BindableBase;
        JObject GetUpdateToken(Type type, BindableBase newEntity, BindableBase? oldEntity = default);
    }
}
