using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.HomeAutomationServer.Crypto
{
    internal class AesKeyProvider:IAesKeyProvider
    {
        public byte[] GetAesKey() => new byte[16];
    }
}
