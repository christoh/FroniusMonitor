﻿namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public abstract class ViewModelBase : BindableBase
{
    protected ViewModelBase()
    {
        NotifiedValidationErrors = new(notifiedValidationErrors);
    }

    public Dispatcher Dispatcher { get; set; } = null!;

    private readonly ObservableCollection<ValidationError> notifiedValidationErrors = new();

    public ReadOnlyObservableCollection<ValidationError> NotifiedValidationErrors { get; }

    public bool HasNotifiedValidationErrors => NotifiedValidationErrors.Count > 0;

    public IEnumerable<ValidationError> VisibleNotifiedValidationErrors => NotifiedValidationErrors.Where(e => ((BindingExpressionBase)e.BindingInError).Target is FrameworkElement { IsVisible: true });

    public bool HasVisibleNotifiedValidationErrors => VisibleNotifiedValidationErrors.Any();

    protected SynchronizationContext Ctx { get; private set; } = null!;

    internal Window View { private get; set; } = null!;



    internal virtual async Task OnInitialize()
    {
        if (SynchronizationContext.Current is null)
        {
            await Dispatcher.InvokeAsync(() => Ctx = SynchronizationContext.Current ?? throw new InvalidOperationException("No Ctx"));
        }
        else
        {
            Ctx = SynchronizationContext.Current;
        }
    }

    internal virtual Task CleanUp() => Task.CompletedTask;

    internal void HandleValidationErrorChange(ValidationErrorEventArgs e)
    {
        switch (e.Action)
        {
            case ValidationErrorEventAction.Added:
                notifiedValidationErrors.Add(e.Error);
                break;
            case ValidationErrorEventAction.Removed:
                notifiedValidationErrors.Remove(e.Error);
                break;
            default:
                throw new NotSupportedException($"{nameof(ValidationErrorEventAction)} {e.Action} is not supported");
        }

        NotifyOfPropertyChange(nameof(HasNotifiedValidationErrors));
        NotifyOfPropertyChange(nameof(HasVisibleNotifiedValidationErrors));
        NotifyOfPropertyChange(nameof(VisibleNotifiedValidationErrors));
    }

    protected virtual void Close() => Dispatcher.InvokeAsync(View.Close);

    protected MessageBoxResult ShowBox(string text, string caption = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None, MessageBoxResult defaultResult = MessageBoxResult.None)
    {
        MessageBoxResult result = defaultResult;

        Dispatcher.Invoke(() =>
        {
            result = MessageBox.Show(View, text, caption, button, icon, defaultResult);
        });

        return result;
    }

    protected IEnumerable<T> FindVisualChildren<T>() where T : DependencyObject => View.FindVisualChildren<T>();
}