namespace De.Hochstaetter.FroniusMonitor.Wpf.Commands;

public abstract class CommandBase : ICommand
{
    private readonly SynchronizationContext? synchronizationContext = SynchronizationContext.Current;

    public abstract bool CanExecute(object? parameter);
    public abstract void Execute(object? parameter);

    public void NotifyCanExecuteChanged()
    {
        if (synchronizationContext == null || SynchronizationContext.Current == synchronizationContext)
        {
            Raise();
            return;
        }

        synchronizationContext.Post(_ => Raise(), null);
    }

    private void Raise()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? CanExecuteChanged;
}

public class Command<T>(Action<T?> action, Func<T?, bool>? canExecuteFunc = null) : CommandBase
{
    public override bool CanExecute(object? parameter)
    {
        return canExecuteFunc?.Invoke(parameter is T t ? t : default) ?? true;
    }

    public override void Execute(object? parameter)
    {
        action(parameter is T t ? t : default);
    }
}

public class NoParameterCommand(Action action, Func<bool>? canExecuteFunc = null) : CommandBase
{
    public override bool CanExecute(object? parameter)
    {
        return canExecuteFunc?.Invoke() ?? true;
    }

    public override void Execute(object? parameter)
    {
        action();
    }
}
