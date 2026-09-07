namespace De.Hochstaetter.Fronius.Services;

public class Gen24JsonService : IGen24JsonService
{
    public T ReadFroniusData<T>(JsonNode? token) where T : new()
    {
        var channels = token?["channels"];
        var attributes = token?["attributes"];

        var result = new T();

        foreach (var propertyInfo in typeof(T).GetProperties().Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(FroniusProprietaryImportAttribute))))
        {
            var value = ReadSingleProperty<T>(token, propertyInfo, attributes, channels);
            propertyInfo.SetValue(result, value);
        }

        return result;
    }

    /// <remarks>
    /// Returns <see cref="object"/> and not <c>dynamic</c>: dynamic dispatch does not exist on iOS. The only
    /// thing it was used for here was the scaling arithmetic at the end, which now says which type it is working
    /// in.
    /// </remarks>
    private object? ReadSingleProperty<T>(JsonNode? token, PropertyInfo propertyInfo, JsonNode? attributes = null, JsonNode? channels = null) where T : new()
    {
        var nonNullablePropertyType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
        var attribute = (FroniusProprietaryImportAttribute)propertyInfo.GetCustomAttributes(typeof(FroniusProprietaryImportAttribute), true).Single();

        var parsedToken = attribute.DataType switch
        {
            FroniusDataType.Attribute => attributes?[attribute.Name],
            FroniusDataType.Root => token?[attribute.Name],

            FroniusDataType.Custom => token?
            [
                attribute.PropertyName ?? throw new ApplicationException($"PropertyName not set for {propertyInfo.Name} in {typeof(T).Name}")
            ]?[attribute.Name],

            _ => channels?[attribute.Name],
        };

        // Everything is read as text and converted from there, because the inverter is not consistent about
        // whether it writes a number as a number or as a string. See JsonExtensions.AsString.
        var stringValue = parsedToken.AsString()?.Trim();
        object? value = null;

        if (stringValue != null)
        {
            if (propertyInfo.PropertyType.IsAssignableFrom(typeof(TimeSpan)))
            {
                var doubleValue = ConvertTo<double>();
                value = TimeSpan.FromSeconds(doubleValue);
            }
            else if (propertyInfo.PropertyType.IsAssignableFrom(typeof(DateTime)))
            {
                if (attribute.Unit is Unit.UnixMilliSeconds or not Unit.ParsableTime)
                {
                    var doubleValue = ConvertTo<double>();
                    value = DateTime.UnixEpoch.AddMilliseconds(doubleValue * (attribute.Unit == Unit.UnixMilliSeconds ? 1d : 1000d));
                }
                else
                {
                    value = !DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateTime) ? null : dateTime;
                }
            }
            else if (attribute.DataType == FroniusDataType.Channel && propertyInfo.PropertyType.IsAssignableFrom(typeof(bool)))
            {
                value = string.IsNullOrEmpty(stringValue) ? null : ConvertTo<int>() != 0;
            }
            else if (propertyInfo.PropertyType.IsAssignableFrom(typeof(Version)))
            {
                if (stringValue.Contains('-'))
                {
                    stringValue = stringValue.Replace("-", ".");
                }

                value = Version.TryParse(stringValue, out var version) ? version : null;
            }
            else if (nonNullablePropertyType.IsEnum)
            {
                value = ReadEnum(nonNullablePropertyType, stringValue);
            }
            else
            {
                value = string.IsNullOrEmpty(stringValue) ? null : Convert.ChangeType(stringValue, nonNullablePropertyType, CultureInfo.InvariantCulture);
            }

            // Joule to watt hours and percent to a fraction. Only for the two floating point types, and only for
            // those two units, so anything else keeps the value exactly as it was converted above.
            if (value is not null && attribute.Unit is Unit.Joule or Unit.Percent &&
                (propertyInfo.PropertyType.IsAssignableFrom(typeof(double)) || propertyInfo.PropertyType.IsAssignableFrom(typeof(float))))
            {
                var scaled = Convert.ToDouble(value, CultureInfo.InvariantCulture) / (attribute.Unit == Unit.Joule ? 3600d : 100d);
                value = nonNullablePropertyType == typeof(float) ? (float)scaled : scaled;
            }
        }

        return value;

        TTarget ConvertTo<TTarget>()
        {
            return (TTarget)Convert.ChangeType(stringValue, typeof(TTarget), CultureInfo.InvariantCulture);
        }
    }

    public object? ReadEnum(Type type, string? stringValue)
    {
        if (stringValue == null)
        {
            return null;
        }

        var fields = type.GetFields();

        if (int.TryParse(stringValue, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _))
        {
            return Enum.Parse(type, stringValue, true);
        }

        var fieldInfo =
            fields.SingleOrDefault(f => f.GetCustomAttributes().Any(a => a is EnumParseAttribute attribute && attribute.ParseAs?.ToUpperInvariant() == stringValue.ToUpperInvariant())) ??
            fields.SingleOrDefault(f => f.GetCustomAttributes().Any(a => a is EnumParseAttribute { IsDefault: true }));

        return fieldInfo == null ? null : Enum.Parse(type, fieldInfo.Name);
    }

    [SuppressMessage("ReSharper", "PreferConcreteValueOverDefault")]
    public JsonObject GetUpdateToken<T>(T newEntity, T? oldEntity = default) where T : BindableBase => GetUpdateToken(typeof(T), newEntity, oldEntity);

    [SuppressMessage("ReSharper", "PreferConcreteValueOverDefault")]
    public JsonObject GetUpdateToken(Type type, BindableBase newEntity, BindableBase? oldEntity = default)
    {
        var jObject = new JsonObject();

        foreach (var propertyInfo in type.GetProperties().Where(p => p.GetCustomAttribute<FroniusProprietaryImportAttribute>() != null))
        {
            var attribute = propertyInfo.GetCustomAttributes<FroniusProprietaryImportAttribute>().Single();
            var jsonValueNew = GetFroniusJsonValue(propertyInfo, newEntity, attribute);

            if (jsonValueNew == null)
            {
                continue;
            }

            if (oldEntity != default)
            {
                var jsonValueOld = GetFroniusJsonValue(propertyInfo, oldEntity, attribute);

                if (jsonValueNew.Equals(jsonValueOld))
                {
                    continue;
                }
            }

            var jsonNode = ToJsonValue(jsonValueNew, propertyInfo);

            if (attribute.DataType != FroniusDataType.Custom || attribute.PropertyName == null)
            {
                jObject.Add(attribute.Name, jsonNode);
            }
            else if (!jObject.TryGetPropertyValue(attribute.PropertyName, out var token))
            {
                jObject.Add(attribute.PropertyName, new JsonObject { [attribute.Name] = jsonNode });
            }
            else
            {
                token![attribute.Name] = jsonNode;
            }
        }

        return jObject;
    }

    /// <summary>
    /// One value on its way to the inverter.
    /// </summary>
    /// <remarks>
    /// Written out by type rather than through <c>JsonValue.Create&lt;object&gt;</c>, which would serialize the
    /// runtime type by reflection - trimmed away on iOS, and silently wrong for anything it does not recognise.
    /// A type nobody has thought about throws instead, so a property added later says so on the first write and
    /// not through an inverter refusing a payload nobody can explain.
    /// </remarks>
    private static JsonNode ToJsonValue(object value, PropertyInfo propertyInfo) => value switch
    {
        string text => JsonValue.Create(text),
        bool flag => JsonValue.Create(flag),
        byte number => JsonValue.Create(number),
        sbyte number => JsonValue.Create(number),
        short number => JsonValue.Create(number),
        ushort number => JsonValue.Create(number),
        int number => JsonValue.Create(number),
        uint number => JsonValue.Create(number),
        long number => JsonValue.Create(number),
        ulong number => JsonValue.Create(number),
        float number => JsonValue.Create(number),
        double number => JsonValue.Create(number),
        decimal number => JsonValue.Create(number),
        _ => throw new NotSupportedException($"{propertyInfo.DeclaringType?.Name}.{propertyInfo.Name} is a {value.GetType().Name}, which cannot be written to an inverter"),
    };

    private static object? GetFroniusJsonValue(PropertyInfo propertyInfo, object instance, FroniusProprietaryImportAttribute attribute)
    {
        var value = propertyInfo.GetValue(instance);

        if (value == null)
        {
            return null;
        }

        var result = value switch
        {
            Enum enumValue => GetFroniusEnumString(enumValue),

            DateTime date => attribute.Unit != Unit.ParsableTime
                ? (long)Math.Round((date.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds, MidpointRounding.AwayFromZero)
                : date.ToString("HH:mm", CultureInfo.InvariantCulture),

            _ => value
        };

        return result;
    }

    private static object? GetFroniusEnumString(Enum enumValue)
    {
        var fieldInfo = enumValue.GetType().GetFields().Single(f => f.Name == enumValue.ToString());

        if (fieldInfo.GetCustomAttributes().SingleOrDefault(a => a is EnumParseAttribute) is not EnumParseAttribute attribute || attribute.ParseNumeric)
        {
            return Convert.ToInt32(enumValue);
        }

        return attribute.ParseAs;
    }
}
