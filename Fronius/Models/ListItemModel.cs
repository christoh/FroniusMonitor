using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models
{
    public class ListItemModel<T> : BindableBase
    {
        private string displayName = string.Empty;

        public string DisplayName
        {
            get => displayName;
            set => Set(ref displayName, value);
        }

        private T? valueField;

        public T? Value
        {
            get => valueField;
            set => Set(ref valueField, value);
        }

        public override string ToString() => DisplayName;
    }
}
