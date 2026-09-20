namespace De.Hochstaetter.HomeAutomationClient.Views.Dialogs;

/// <summary>
/// The body of the Solar.web chart dialog. What is in the code behind is exactly the two things the interaction rule
/// leaves there: drawing the view model's <see cref="SolarWebChartModel"/> with the charting library, and reading
/// the theme's colours for it - both need the UI framework, and neither decides anything.
/// </summary>
public partial class SolarWebChartView : UserControl, IDialogControl
{
    private SolarWebChartViewModel? viewModel;

    public SolarWebChartView()
    {
        // Before the AvaPlot is built: a plot takes its font at construction, and it has to be the app's Inter,
        // not whatever Skia finds on the platform - see InterFontResolver.
        InterFontResolver.Register();
        InitializeComponent();

        // Zoom and pan are off, as in the price chart: a day is a day, and dragging it about only loses the labels.
        Plot.UserInputProcessor.IsEnabled = false;

        DataContextChanged += OnDataContextChanged;
        ActualThemeVariantChanged += (_, _) => Render();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (viewModel != null)
        {
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        viewModel = DataContext as SolarWebChartViewModel;

        if (viewModel == null)
        {
            return;
        }

        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Render();

        // Nobody can await it here; Initialize guards itself against running twice and reports its own failures
        // through TaskExceptionHandler, and HandleTaskExceptions catches whatever escapes that all the same.
        ViewModelBase.HandleTaskExceptions(viewModel.Initialize);
    }

    /// <summary>The model is set from the web client's continuation, which is the UI thread; the post keeps it so whatever the thread.</summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SolarWebChartViewModel.ChartModel))
        {
            Dispatcher.UIThread.Post(Render);
        }
    }

    private void Render()
    {
        // A fresh plot every time: the right axis and the manual ticks would otherwise survive from the last drawing.
        Plot.Reset();

        if (viewModel?.ChartModel is { } model)
        {
            SolarWebChartRenderer.Render(Plot.Plot, model, ChartTheme.PaletteOf(this));
        }

        Plot.Refresh();
    }
}
