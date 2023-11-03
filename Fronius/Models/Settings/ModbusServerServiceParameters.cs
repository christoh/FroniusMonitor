using DotNetty.Transport.Bootstrapping;

namespace De.Hochstaetter.Fronius.Models.Settings;

public class ModbusServerServiceParameters
{
    public IPEndPoint EndPoint { get; set; } = IPEndPoint.Parse("[::]:502");
    public ICollection<ModbusMapping> Mappings { get; set; } = new List<ModbusMapping>();
    public bool AutoMap { get; set; }
}