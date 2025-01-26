namespace De.Hochstaetter.HomeAutomationServer.Misc;

internal static class Helpers
{
    public static ValidationProblemDetails GetValidationDetails(string key, string message)
    {
        return new ValidationProblemDetails(new Dictionary<string, string[]>([new KeyValuePair<string, string[]>(key, [message])]));
    }
}