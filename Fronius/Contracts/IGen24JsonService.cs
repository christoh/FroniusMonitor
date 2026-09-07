namespace De.Hochstaetter.Fronius.Contracts;

public interface IGen24JsonService
{
    T ReadFroniusData<T>(JsonNode? device) where T : new();
    object? ReadEnum(Type type, string? stringValue);
    JsonObject GetUpdateToken<T>(T newEntity, T? oldEntity = default) where T : BindableBase;
    JsonObject GetUpdateToken(Type type, BindableBase newEntity, BindableBase? oldEntity = default);
}
