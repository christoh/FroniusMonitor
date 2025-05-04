namespace De.Hochstaetter.Fronius.Models.HomeAutomationClient;

public class ApiResult<T> : ProblemDetails
{
    public T? Payload { get; set; }

    public Exception? Exception { get; set; }

    public static ApiResult<T> FromProblemDetails(ProblemDetails? problemDetails, HttpStatusCode? httpStatusCode, Exception? ex = null)
    {
        var result = new ApiResult<T>
        {
            Payload = default,
            Detail = problemDetails?.Detail,
            Title = problemDetails?.Title,
            Status = problemDetails?.Status ?? httpStatusCode,
            Errors = problemDetails?.Errors ?? new Dictionary<string, List<string>> { { "Errors", [problemDetails?.Detail ?? "Unknown Error"] } },
            Exception = ex,
        };

        return result;
    }
}