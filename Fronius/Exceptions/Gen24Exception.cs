namespace De.Hochstaetter.Fronius.Exceptions;

public class Gen24Exception(int status, string message, string userMessage, string urlString)
    : Exception(message)
{
    public int Status { get; } = status;
    public string UserMessage { get; } = userMessage;
    public string UrlString { get; } = urlString;
}