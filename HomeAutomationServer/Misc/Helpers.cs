namespace De.Hochstaetter.HomeAutomationServer.Misc;

internal static class Helpers
{
    public static ProblemDetails GetProblemDetails(string? title, string? message)
    {
        return new ProblemDetails { Detail = message, Title = title, };
    }
}