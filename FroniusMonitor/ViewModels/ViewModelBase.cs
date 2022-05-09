using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using De.Hochstaetter.Fronius.Models;

namespace De.Hochstaetter.FroniusMonitor.ViewModels
{
    public abstract class ViewModelBase:BindableBase
    {
        public Dispatcher Dispatcher { get; set; } = null!;

        internal virtual Task OnInitialize() => Task.CompletedTask;
    }
}
