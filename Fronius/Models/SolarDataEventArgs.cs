namespace De.Hochstaetter.Fronius.Models;

public class SolarDataEventArgs(HomeAutomationSystem? homeAutomationSystem) : EventArgs
{
    public HomeAutomationSystem? HomeAutomationSystem { get; set; } = homeAutomationSystem;
}