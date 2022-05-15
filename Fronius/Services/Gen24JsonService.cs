namespace De.Hochstaetter.Fronius.Services;

public class Gen24JsonService : IGen24JsonService
{
    public T ReadFroniusData<T>(JToken? device) where T : new()
    {
        var channels = device?["channels"];
        var attributes = device?["attributes"];

        var result = new T();

        foreach (var propertyInfo in typeof(T).GetProperties().Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(FroniusProprietaryImportAttribute))))
        {
            var nonNullablePropertyType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
            var attribute = (FroniusProprietaryImportAttribute)propertyInfo.GetCustomAttributes(typeof(FroniusProprietaryImportAttribute), true).Single();

            var token = attribute.DataType switch
            {
                FroniusDataType.Attribute => attributes?[attribute.Name],
                FroniusDataType.Root => device?[attribute.Name],
                _ => channels?[attribute.Name],
            };

            var stringValue = token?.Value<string>()?.Trim();
            dynamic? value = null;

            if (stringValue != null)
            {
                if (propertyInfo.PropertyType.IsAssignableFrom(typeof(TimeSpan)))
                {
                    var doubleValue = (double)Convert.ChangeType(stringValue, typeof(double), CultureInfo.InvariantCulture);
                    value = TimeSpan.FromSeconds(doubleValue);
                }
                else if (propertyInfo.PropertyType.IsAssignableFrom(typeof(DateTime)))
                {
                    var doubleValue = (double)Convert.ChangeType(stringValue, typeof(double), CultureInfo.InvariantCulture);
                    value = DateTime.UnixEpoch.AddSeconds(doubleValue);
                }
                else if (attribute.DataType == FroniusDataType.Channel && propertyInfo.PropertyType.IsAssignableFrom(typeof(bool)))
                {
                    value = string.IsNullOrEmpty(stringValue) ? null : (double)Convert.ChangeType(stringValue, typeof(double), CultureInfo.InvariantCulture) != 0d;
                }
                else if (propertyInfo.PropertyType.IsAssignableFrom(typeof(Version)))
                {
                    try
                    {
                        value = new Version(stringValue);
                    }
                    catch
                    {
                        value = null;
                    }
                }
                else if (nonNullablePropertyType.IsEnum)
                {
                    value = ReadEnum(nonNullablePropertyType, stringValue);
                }
                else
                {
                    value = string.IsNullOrEmpty(stringValue) ? null : Convert.ChangeType(stringValue, nonNullablePropertyType, CultureInfo.InvariantCulture);
                }

                if (propertyInfo.PropertyType.IsAssignableFrom(typeof(double)) || propertyInfo.PropertyType.IsAssignableFrom(typeof(float)))
                {
                    value = attribute.Unit switch
                    {
                        Unit.Joule => value / 3600,
                        Unit.Percent => value / 100,
                        _ => value,
                    };
                }
            }

            propertyInfo.SetValue(result, value);
        }

        return result;
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
            fields.SingleOrDefault(f => f.GetCustomAttributes().Any(a => a is EnumParseAttribute {IsDefault: true}));

        return fieldInfo == null ? null : Enum.Parse(type, fieldInfo.Name);
    }

    public JObject GetUpdateToken<T>(T newEntity, T? oldEntity = default) where T : BindableBase
    {
        var jObject = new JObject();

        foreach (var propertyInfo in typeof(T).GetProperties().Where(p=>p.GetCustomAttribute<FroniusProprietaryImportAttribute>()!=null))
        {
            var jsonValueNew = GetFroniusJsonValue(propertyInfo, newEntity);

            if (jsonValueNew == null)
            {
                continue;
            }

            if (oldEntity != default)
            {
                var jsonValueOld = GetFroniusJsonValue(propertyInfo, oldEntity);

                if (jsonValueNew.Equals(jsonValueOld))
                {
                    continue;
                }
            }

            var attribute = propertyInfo.GetCustomAttributes<FroniusProprietaryImportAttribute>().Single();
            jObject.Add(attribute.Name, new JValue(jsonValueNew));
        }

        return jObject;
    }

    private object? GetFroniusJsonValue(PropertyInfo propertyInfo, object instance)
    {
        var value = propertyInfo.GetValue(instance);

        if (value == null)
        {
            return null;
        }

        var result = value switch
        {
            Enum enumValue => GetFroniusEnumString(enumValue),
            DateTime date => (long)Math.Round((date.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds, MidpointRounding.AwayFromZero),
            _ => value
        };

        return result;
    }

    private object? GetFroniusEnumString(Enum enumValue)
    {
        var fieldInfo = enumValue.GetType().GetFields().Single(f => f.Name == enumValue.ToString());

        if (fieldInfo.GetCustomAttributes().SingleOrDefault(a => a is EnumParseAttribute) is not EnumParseAttribute attribute || attribute.ParseNumeric)
        {
            return Convert.ToInt32(enumValue);
        }

        return attribute.ParseAs;
    }
}
