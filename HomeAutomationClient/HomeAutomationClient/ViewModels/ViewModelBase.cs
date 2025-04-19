using System.Diagnostics.CodeAnalysis;
using De.Hochstaetter.Fronius.Models;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public abstract partial class ViewModelBase : BindableBase
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsBusy))]
    public partial string? BusyText { get; set; }

    public bool IsBusy => BusyText != null;

    public virtual Task Initialize() => Task.CompletedTask;
}