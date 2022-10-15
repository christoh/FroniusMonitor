using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Contracts
{
    public interface IAesKeyProvider
    {
        public byte[] GetAesKey();
    }
}
