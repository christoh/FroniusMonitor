using De.Hochstaetter.Fronius.Services;

namespace De.Hochstaetter.Fronius.Extensions;

public static class WattPilotExtensions
{
    private static readonly ILogger<WattPilotService>? logger=IoC.TryGetRegistered<ILogger<WattPilotService>>();
    
    extension(WattPilot instance)
    {
        public void UpdateFromJson(JsonObject jObject) => ParseUpdateToken(instance, jObject);
        public void UpdateFromJson(string json) => ParseUpdateToken(instance, JsonNode.Parse(json)?.AsObject() ?? []);
    }

    /// <summary>A property value as the WattPilot expects to be sent it.</summary>
    /// <remarks>
    /// The write side of the format <see cref="UpdateFromJson(string)"/> reads, which is why it lives here and
    /// not in the service: a name the charger uses is written and read in one place.
    /// </remarks>
    public static JsonNode? ToWattPilotJson(object? value, WattPilotAttribute attribute) => value switch
    {
        null => null,
        bool boolValue => JsonValue.Create(boolValue),
        byte byteValue => JsonValue.Create(byteValue),
        uint uintValue => JsonValue.Create(uintValue),
        long longValue => JsonValue.Create(longValue),
        Enum enumValue => JsonValue.Create((int)Convert.ChangeType(enumValue, TypeCode.Int32)),
        int intValue => JsonValue.Create(intValue),
        string stringValue => JsonValue.Create(stringValue),
        double doubleValue => JsonValue.Create(doubleValue),
        float floatValue => JsonValue.Create(floatValue),
        IEnumerable<byte> bytes => new JsonArray([.. bytes.Select(b => (JsonNode?)(int)b)]),
        IEnumerable<int> integers => new JsonArray([.. integers.Select(number => (JsonNode?)number)]),
        _ when attribute.Type?.IsInstanceOfType(value) is true => ToWattPilotObject(value),
        _ => throw new NotSupportedException("Unsupported Type"),
    };

    /// <summary>
    /// A nested object - the load balancing currents, or a WiFi - as the WattPilot expects to be sent it.
    /// </summary>
    /// <remarks>
    /// Written from the <see cref="WattPilotAttribute"/>s of the object, which is what it is read back through,
    /// so the names the charger sees are stated once. Newtonsoft used to write this from its own
    /// <c>JsonProperty</c> names - the same names kept a second time - and sent every property that carried no
    /// name of its own out under its C# name, which the charger has no use for.
    /// </remarks>
    private static JsonObject ToWattPilotObject(object instance)
    {
        var result = new JsonObject();

        foreach (var propertyInfo in instance.GetType().GetProperties())
        {
            // An indexed attribute names one slot of an array the charger sends, so it is not a property that
            // can be written back on its own. Only a plain name is.
            var attribute = propertyInfo.GetCustomAttributes<WattPilotAttribute>().FirstOrDefault(a => a.Index < 0);

            if (attribute is not null)
            {
                result[attribute.TokenName] = ToWattPilotJson(propertyInfo.GetValue(instance), attribute);
            }
        }

        return result;
    }

    /// <summary>Whether this is a type whose JSON names are stated by <see cref="WattPilotAttribute"/>.</summary>
    private static bool IsWattPilotObject(Type type) => type.GetProperties().Any(p => p.GetCustomAttributes<WattPilotAttribute>().Any());

    /// <summary>The type of one entry of an array or a list, or <see langword="null"/> where it is neither.</summary>
    private static Type? ElementTypeOf(Type type) => type.IsArray
        ? type.GetElementType()
        : type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type)
            ? type.GetGenericArguments().SingleOrDefault()
            : null;

    private static object? ReadWattPilotObject(Type type, JsonObject jsonObject)
    {
        var result = Activator.CreateInstance(type);

        if (result is not null)
        {
            ParseUpdateToken(result, jsonObject);
        }

        return result;
    }

    private static void ParseUpdateToken(object instance, JsonObject jObject)
    {
        foreach (var token in jObject)
        {
            if (logger?.IsEnabled(LogLevel.Debug) is true)
            {
                logger.LogTrace("{Key}: {Value}", token.Key, token.Value?.ToString().Replace("\r", "").Replace("\n", ""));
            }

            var propertyInfos = instance.GetType().GetProperties().Where(p => p.GetCustomAttributes<WattPilotAttribute>().Any(a => a.TokenName == token.Key)).ToArray();

            switch (propertyInfos.Length)
            {
                case 0:
                    continue;

                case 1:
                    SetWattPilotValue(instance, propertyInfos[0], token.Value);
                    continue;
            }

            if (token.Value is JsonArray array)
            {
                foreach (var propertyInfo in propertyInfos)
                {
                    var attribute = propertyInfo.GetCustomAttributes<WattPilotAttribute>().SingleOrDefault(a => a.TokenName == token.Key);

                    if (attribute?.Index >= 0 && attribute.Index < array.Count)
                    {
                        SetWattPilotValue(instance, propertyInfo, array[attribute.Index]);
                    }
                }
            }
        }
    }

    private static void SetWattPilotValue(object instance, PropertyInfo propertyInfo, JsonNode? token)
    {
        // A nested object, and a list of them, carries its names on WattPilotAttribute like everything else here,
        // so it is read the way its parent is and never handed to the serializer. Deserialize would take such an
        // object without complaining and answer one with every property left at its default, because none of the
        // names mean anything to it - the charger would appear to be reporting zeroes. Newtonsoft got this right
        // by accident, from a second copy of the names kept on its own attributes.
        switch (token)
        {
            case JsonObject jsonObject when IsWattPilotObject(propertyInfo.PropertyType):
                propertyInfo.SetValue(instance, ReadWattPilotObject(propertyInfo.PropertyType, jsonObject));
                return;

            // A byte array is base 64 to System.Text.Json, in both directions. The charger writes the phase map
            // as an array of numbers, so it is read as one - the same way ToWattPilotJson writes it back.
            case JsonArray jsonArray when ElementTypeOf(propertyInfo.PropertyType) == typeof(byte):
                propertyInfo.SetValue(instance, jsonArray.Select(entry => (byte)(entry.AsInt32() ?? 0)).ToArray());
                return;

            case JsonArray jsonArray when ElementTypeOf(propertyInfo.PropertyType) is { } elementType && IsWattPilotObject(elementType):
                var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
                jsonArray.Apply(entry => list.Add(entry is JsonObject entryObject ? ReadWattPilotObject(elementType, entryObject) : null));
                propertyInfo.SetValue(instance, list);
                return;
        }

        try
        {
            propertyInfo.SetValue(instance, JsonSerializer.Deserialize(token, propertyInfo.PropertyType));
            return;
        }
        catch
        {
            //
        }

        if (token is JsonObject subObject)
        {
            var subInstance = Activator.CreateInstance(propertyInfo.PropertyType);

            if (subInstance != null)
            {
                propertyInfo.SetValue(instance, subInstance);
                ParseUpdateToken(subInstance, subObject);
                return;
            }
        }

        var stringValue = token.AsString();

        if (propertyInfo.PropertyType.IsAssignableFrom(typeof(IPAddress)))
        {
            propertyInfo.SetValue(instance, stringValue == null ? null : IPAddress.Parse(stringValue));
            return;
        }

        // The text fallback exists because System.Text.Json is strict where Newtonsoft coerced: ToObject read a
        // JSON string "5" into an int and Deserialize refuses to, so without this a charger that writes a number
        // as text would quietly leave the property unset. It is the same "read it as text and convert" the
        // inverter side does - see Gen24JsonService.
        if (stringValue != null)
        {
            var targetType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

            try
            {
                propertyInfo.SetValue(instance, targetType switch
                {
                    // Convert.ToBoolean refuses "1", and a charger that writes a flag as a number is exactly what
                    // this fallback is here for.
                    _ when targetType == typeof(bool) => token.AsBoolean(),
                    _ when targetType.IsEnum => IoC.Get<IGen24JsonService>().ReadEnum(targetType, stringValue),
                    _ => Convert.ChangeType(stringValue, targetType, CultureInfo.InvariantCulture),
                });

                return;
            }            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
            {
                //
            }
        }

        logger?.LogWarning("Cannot read {Property} of a WattPilot from {Json}", propertyInfo.Name, token?.ToJsonString() ?? "null");
    }
}