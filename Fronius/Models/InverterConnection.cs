using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models
{
    public class InverterConnection : BindableBase
    {
        public string BaseUrl { get; init; } = string.Empty;
    }
}
