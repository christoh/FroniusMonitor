namespace De.Hochstaetter.Fronius.Exceptions
{
    public class SolarException : Exception
    {
        public int Status { get; }
        public string UserMessage { get; }
        public string UrlString { get; }

        public SolarException(int status, string message, string userMessage, string urlString) : base(message)
        {
            Status = status;
            UserMessage = userMessage;
            UrlString = urlString;
        }
    }
}
