namespace De.Hochstaetter.FroniusMonitor.Controls;

public class ValidationWindow : Window
{
    protected void OnValidationChanged(object? sender, ValidationErrorEventArgs e)
    {
        if (DataContext is ViewModelBase viewModelBase)
        {
            viewModelBase.HandleValidationErrorChange(e);
        }
    }
}
