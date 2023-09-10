namespace De.Hochstaetter.Fronius.Models;

public class SolarDataEventArgs : EventArgs
{
    public SolarDataEventArgs(HomeAutomationSystem? homeAutomationSystem)
    {
        HomeAutomationSystem = homeAutomationSystem;
    }

    public HomeAutomationSystem? HomeAutomationSystem { get; set; }
}