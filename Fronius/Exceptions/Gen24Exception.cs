namespace De.Hochstaetter.Fronius.Exceptions;

public class Gen24Exception : Exception
{
    public int Status { get; }
    public string UserMessage { get; }
    public string UrlString { get; }

    public Gen24Exception(int status, string message, string userMessage, string urlString) : base(message)
    {
            Status = status;
            UserMessage = userMessage;
            UrlString = urlString;
        }
}