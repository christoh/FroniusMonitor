using System.Diagnostics.CodeAnalysis;
using De.Hochstaetter.Fronius.Models;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public abstract class ViewModelBase : BindableBase
{
    public virtual Task Initialize() => Task.CompletedTask;
}