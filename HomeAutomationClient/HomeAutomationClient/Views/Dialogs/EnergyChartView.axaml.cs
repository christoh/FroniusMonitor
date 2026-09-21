namespace De.Hochstaetter.HomeAutomationClient.Views.Dialogs;

/// <summary>
/// The body of the price chart dialog. What is in the code behind is exactly the two things the interaction rule
/// leaves there: drawing the view model's <see cref="EnergyChartModel"/> with the charting library, and reading
/// the theme's colors for it - both need the UI framework, and neither decides anything.
/// </summary>
public partial class EnergyChartView : UserControl, IDialogControl
{
    private EnergyChartViewModel? viewModel;

    public EnergyChartView()
    {
        // Before the AvaPlot is built: a plot takes its font at construction, and it has to be the app's Inter,
        // not whatever Skia finds on the platform - see InterFontResolver.
        InterFontResolver.Register();
        InitializeComponent();

        // The WPF chart had zoom and pan switched off on both axes: a day is a day, and dragging it about only
        // loses the user the labels. The same here.
        Plot.UserInputProcessor.IsEnabled = false;

        DataContextChanged += OnDataContextChanged;
        ActualThemeVariantChanged += (_, _) => Render();
        // Colour the empty plot as soon as the control is in the tree with a real theme, which is before there
        // is a model to draw.
        Loaded += (_, _) => Render();

        // The view model leaves a push alone while the chart cannot be seen and rebuilds once it can.
        ViewVisibility.Follow(this);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (viewModel != null)
        {
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        viewModel = DataContext as EnergyChartViewModel;

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

    /// <summary>
    /// A new model may be set from the hub's thread when the server pushes; a control is drawn on the UI thread.
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EnergyChartViewModel.ChartModel))
        {
            Dispatcher.UIThread.Post(Render);
        }
    }

    private void Render()
    {
        // A fresh plot every time: the weather adds axes of its own, and clearing the plottables alone would
        // leave the axes of the last drawing standing.
        Plot.Reset();

        var palette = ChartTheme.PaletteOf(this);

        if (viewModel?.ChartModel is { } model)
        {
            EnergyChartRenderer.Render(Plot.Plot, model, palette);
        }
        else
        {
            // ScottPlot's default figure is white. Colour it from the theme even while there is nothing to draw,
            // so a dark dialog does not flash white until the chart is ready.
            ChartTheme.Apply(Plot.Plot, palette);
        }

        Plot.Refresh();
    }
}
