namespace De.Hochstaetter.FroniusShared.Models;

public class SolarDataEventArgs : EventArgs
{
    public SolarDataEventArgs(HomeAutomationSystem? homeAutomationSystem)
    {
        HomeAutomationSystem = homeAutomationSystem;
    }

    public HomeAutomationSystem? HomeAutomationSystem { get; set; }
}
