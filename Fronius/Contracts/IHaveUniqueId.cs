namespace De.Hochstaetter.Fronius.Contracts;

public interface IHaveUniqueId
{
    string Id => string.Join('‖', new[] { Manufacturer, Model, SerialNumber }.Where(s => !string.IsNullOrEmpty(s)));
        
    bool IsPresent { get; }
        
    string? Manufacturer { get; }
        
    public string? Model { get; }
        
    public string? SerialNumber { get; }
}