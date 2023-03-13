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

                FroniusDataType.Custom => device?
                [
                    attribute?.PropertyName ?? throw new ApplicationException($"PropertyName not set for {propertyInfo.Name} in {typeof(T).Name}")
                ]?[attribute.Name],

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
                    if (attribute.Unit != Unit.Time)
                    {
                        var doubleValue = (double)Convert.ChangeType(stringValue, typeof(double), CultureInfo.InvariantCulture);
                        value = DateTime.UnixEpoch.AddSeconds(doubleValue);
                    }
                    else
                    {
                        if (!DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateTime))
                        {
                            value = null;
                        }
                        else
                        {
                            value = dateTime;
                        }
                    }
                }
                else if (attribute.DataType == FroniusDataType.Channel && propertyInfo.PropertyType.IsAssignableFrom(typeof(bool)))
                {
                    value = string.IsNullOrEmpty(stringValue) ? null : (double)Convert.ChangeType(stringValue, typeof(double), CultureInfo.InvariantCulture) != 0d;
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
            fields.SingleOrDefault(f => f.GetCustomAttributes().Any(a => a is EnumParseAttribute { IsDefault: true }));

        return fieldInfo == null ? null : Enum.Parse(type, fieldInfo.Name);
    }

    public JObject GetUpdateToken<T>(T newEntity, T? oldEntity = default) where T : BindableBase
    {
        var jObject = new JObject();

        foreach (var propertyInfo in typeof(T).GetProperties().Where(p => p.GetCustomAttribute<FroniusProprietaryImportAttribute>() != null))
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

            if (attribute.DataType != FroniusDataType.Custom || attribute.PropertyName == null)
            {
                jObject.Add(attribute.Name, new JValue(jsonValueNew));
            }
            else
            {
                if (!jObject.ContainsKey(attribute.PropertyName))
                {
                    jObject.Add(attribute.PropertyName, new JObject { { attribute.Name, new JValue(jsonValueNew) } });
                }
                else
                {
                    jObject[attribute.PropertyName]![attribute.Name] = new JValue(jsonValueNew);
                }
            }
        }

        return jObject;
    }

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

            DateTime date => attribute.Unit != Unit.Time
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
