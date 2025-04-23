namespace De.Hochstaetter.Fronius.Models.HomeAutomationClient;

public class ApiResult<T> : ProblemDetails
{
    public T? Payload { get; set; }
        
    public Exception? Exception { get; set; }

    public static ApiResult<T> FromProblemDetails(ProblemDetails problemDetails)
    {
        var result = new ApiResult<T>();
        typeof(ProblemDetails).GetProperties().Apply(pi => pi.SetValue(result, pi.GetValue(problemDetails)));
        return result;
    }
}