namespace De.Hochstaetter.Fronius.Models.Settings;

public class Gen24DataCollectorParameters : PolledWebConnectionParameterBase
{
    public TimeSpan ConfigRefreshRate { get; set; } = TimeSpan.FromMinutes(5);
    public string LogDirectory { get; set; } = "/var/log";
}