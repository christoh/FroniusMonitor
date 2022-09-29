using System.Collections.ObjectModel;

namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public abstract class ViewModelBase:BindableBase
{
    public Dispatcher Dispatcher { get; set; } = null!;

    private readonly ObservableCollection<ValidationError> notifiedValidationErrors  = new();
    public ReadOnlyObservableCollection<ValidationError> NotifiedValidationErrors { get; }

    public bool HasNotifiedValidationErrors => NotifiedValidationErrors.Count > 0;

    public IReadOnlyList<ValidationError> VisibleNotifiedValidationErrors=>NotifiedValidationErrors.Where(e=>((BindingExpression)e.BindingInError).Target is FrameworkElement{Visibility: Visibility.Visible}).ToArray();

    protected ViewModelBase()
    {
        NotifiedValidationErrors = new(notifiedValidationErrors);
    }
    
    internal virtual Task OnInitialize() => Task.CompletedTask;

    public void HandleValidationErrorChange(ValidationErrorEventArgs e)
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
        NotifyOfPropertyChange(nameof(VisibleNotifiedValidationErrors));
    }
}