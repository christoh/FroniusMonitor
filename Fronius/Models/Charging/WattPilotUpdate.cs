namespace De.Hochstaetter.Fronius.Models.Charging;

public class WattPilotUpdate(string serialNumber, string jsonMessage) : IHaveUniqueId
{
    public bool IsPresent => true;
    public string Manufacturer => "Fronius";
    public string Model => "WattPilotUpdateMessage";

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string SerialNumber => serialNumber;

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string JsonMessage => jsonMessage;
}
