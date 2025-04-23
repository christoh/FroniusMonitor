namespace De.Hochstaetter.HomeAutomationClient.Models;

public class HomeAutomationServerConnection : WebConnection
{
    public HomeAutomationServerConnection()
    {
        IsSlowPlatform = true;
    }
}
