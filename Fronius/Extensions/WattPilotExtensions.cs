namespace De.Hochstaetter.Fronius.Extensions;

public static class WattPilotExtensions
{
    public static void UpdateFromJObject(this WattPilot instance, JObject jObject) => ParseUpdateToken(instance, jObject);

    public static void UpdateFromJsonString(this WattPilot instance, string json) => ParseUpdateToken(instance, JObject.Parse(json));

    private static void ParseUpdateToken(object instance, JObject jObject)
    {
        foreach (var token in jObject)
        {
            // Debug.Print($"{token.Key}: {token.Value?.ToString().Replace("\r", "").Replace("\n", "")}");
            var propertyInfos = instance.GetType().GetProperties().Where(p => p.GetCustomAttributes<WattPilotAttribute>().Any(a => a.TokenName == token.Key)).ToArray();

            switch (propertyInfos.Length)
            {
                case 0:
                    continue;

                case 1:
                    SetWattPilotValue(instance, propertyInfos[0], token.Value);
                    continue;
            }

            if (token.Value is JArray array)
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

    private static void SetWattPilotValue(object instance, PropertyInfo propertyInfo, JToken? token)
    {
        try
        {
            propertyInfo.SetValue(instance, token?.ToObject(propertyInfo.PropertyType));
            return;
        }
        catch
        {
            //
        }

        if (token is JObject subObject)
        {
            var subInstance = Activator.CreateInstance(propertyInfo.PropertyType);

            if (subInstance != null)
            {
                propertyInfo.SetValue(instance, subInstance);
                ParseUpdateToken(subInstance, subObject);
                return;
            }
        }

        var stringValue = token?.Value<string>();

        if (propertyInfo.PropertyType.IsAssignableFrom(typeof(IPAddress)))
        {
            propertyInfo.SetValue(instance, stringValue == null ? null : IPAddress.Parse(stringValue));
            return;
        }

        Debugger.Break(); // Unhandled Json
    }
}