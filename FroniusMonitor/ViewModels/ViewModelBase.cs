namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public abstract class ViewModelBase:BindableBase
{
    public Dispatcher Dispatcher { get; set; } = null!;

    internal virtual Task OnInitialize() => Task.CompletedTask;
}