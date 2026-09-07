using Avalonia.VisualTree;

namespace De.Hochstaetter.HomeAutomationClient.Views.Dialogs;

public partial class Gen24ModbusView : UserControl
{
    public Gen24ModbusView() => InitializeComponent();

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is Gen24ModbusViewModel viewModel)
        {
            viewModel.GetInvalidFields = GetInvalidFields;
        }
    }

    /// <summary>
    /// The values the settings refused, read off the fields that hold them. Lives in the code behind because the
    /// visual tree and the validation state of a control are exactly what a view model must not touch; it hands
    /// the view model plain strings, the way <c>DashboardView</c> hands it plain colors.
    /// </summary>
    /// <remarks>
    /// A field that is not on screen is left out on purpose. Its setter threw, so the model still holds the value
    /// the dialog read, and an error the user cannot even see must not stop them saving - the dialog of
    /// FroniusMonitor took the same view, it just had to write the old value back by hand to get there.
    /// </remarks>
    private IReadOnlyList<string> GetInvalidFields() =>
    [
        .. this.GetVisualDescendants()
            .OfType<TextBox>()
            .Where(textBox => textBox.IsEffectivelyVisible)
            .SelectMany(textBox => DataValidationErrors.GetErrors(textBox) ?? [])
            .Select(error => error is Exception exception ? exception.Message : error?.ToString())
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Select(message => message!)
            .Distinct(),
    ];
}
