using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Contracts
{
    public interface IGen24JsonService
    {
        T ReadFroniusData<T>(JToken? device) where T : new();
        object? ReadEnum(Type type, string? stringValue);
    }
}
