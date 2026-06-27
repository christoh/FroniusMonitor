using De.Hochstaetter.Fronius.Services;

namespace De.Hochstaetter.Fronius.Extensions;

public static class WattPilotExtensions
{
    private static readonly ILogger<WattPilotService>? logger=IoC.TryGetRegistered<ILogger<WattPilotService>>();
    
    extension(WattPilot instance)
    {
        public void UpdateFromJson(JObject jObject) => ParseUpdateToken(instance, jObject);
        public void UpdateFromJson(string json) => ParseUpdateToken(instance, JObject.Parse(json));
    }

    private static void ParseUpdateToken(object instance, JObject jObject)
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