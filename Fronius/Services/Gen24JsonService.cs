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
}
