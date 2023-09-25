namespace De.Hochstaetter.FroniusMonitor.ViewModels;

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

    internal Window View { private get; set; } = null!;

    internal virtual Task OnInitialize() => Task.CompletedTask;

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
                throw new NotImplementedException($"{nameof(ValidationErrorEventAction)} {e.Action} is not yet implemented");
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