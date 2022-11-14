using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FroniusPhone.Services
{
    internal class AesKeyProvider:IAesKeyProvider
    {
        public byte[] GetAesKey()
        {
            return new byte[16];
        }
    }
}
