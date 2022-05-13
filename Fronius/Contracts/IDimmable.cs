using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Contracts
{
    public interface IDimmable
    {
        bool CanDim { get; }
        double? Level { get; }
        Task SetLevel(double level);
        bool IsDimmingEnabled { get; }
    }
}
