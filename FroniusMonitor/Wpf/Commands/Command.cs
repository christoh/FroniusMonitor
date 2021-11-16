using System.Windows.Input;

namespace De.Hochstaetter.FroniusMonitor.Wpf.Commands
{
    public abstract class CommandBase : ICommand
    {
        private readonly SynchronizationContext? synchronizationContext;

        protected CommandBase()
        {
            synchronizationContext = SynchronizationContext.Current;
        }

        public abstract bool CanExecute(object? parameter);
        public abstract void Execute(object? parameter);

        public void NotifyCanExecuteChanged()
        {
            if (synchronizationContext == null || SynchronizationContext.Current == synchronizationContext)
            {
                Raise();
                return;
            }

            synchronizationContext.Post(d => Raise(), null);
        }

        private void Raise()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? CanExecuteChanged;
    }

    public class Command<T> : CommandBase
    {
        private readonly Action<T?> action;
        private readonly Func<T?, bool>? canExecuteFunc;

        public Command(Action<T?> action, Func<T?, bool>? canExecuteFunc = null)
        {
            this.action = action;
            this.canExecuteFunc = canExecuteFunc;
        }

        public override bool CanExecute(object? parameter)
        {
            return canExecuteFunc?.Invoke(parameter is T t ? t : default) ?? true;
        }

        public override void Execute(object? parameter)
        {
            action(parameter is T t ? t : default);
        }
    }

    public class NoParameterCommand : CommandBase
    {
        private readonly Action action;
        private readonly Func<bool>? canExecuteFunc;

        public NoParameterCommand(Action action, Func<bool>? canExecuteFunc = null)
        {
            this.action = action;
            this.canExecuteFunc = canExecuteFunc;
        }

        public override bool CanExecute(object? parameter)
        {
            return canExecuteFunc?.Invoke() ?? true;
        }

        public override void Execute(object? parameter)
        {
            action();
        }
    }
}
