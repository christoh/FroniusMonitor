using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models
{
    public class DeviceInfo:BindableBase
    {
        private DeviceClass deviceClass;
        public DeviceClass DeviceClass
        {
            get=>deviceClass;
            set=>Set(ref deviceClass,value);
        }

        private string? serialNumber;
        public string? SerialNumber
        {
            get=>serialNumber;
            set=>Set(ref serialNumber,value);
        }

        private int id;
        public int Id
        {
            get=>id;
            set=>Set(ref id,value);
        }
    }
}
