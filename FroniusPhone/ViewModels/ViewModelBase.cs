using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FroniusPhone.ViewModels;

public abstract class ViewModelBase : BindableBase
{
    public IDispatcher Dispatcher { protected get; set; } = null!;
}