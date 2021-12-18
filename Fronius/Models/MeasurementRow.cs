using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using De.Hochstaetter.Fronius.Contracts;

namespace De.Hochstaetter.Fronius.Models
{
    public class MeasurementRow : BindableBase, IHaveDisplayName
    {
        private string displayName=string.Empty;

        public string DisplayName
        {
            get => displayName;
            set => Set(ref displayName, value);
        }

        private IConvertible? value;

        public IConvertible? Value
        {
            get => value;
            set => Set(ref value, value);
        }
    }
}
