using De.Hochstaetter.Fronius.Models;

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

        if (viewModel?.ChartModel is { } model)
        {
            EnergyChartRenderer.Render(Plot.Plot, model, new EnergyChartPalette(ThemeColor("ForegroundBrush", HaColors.Black), ThemeColor("DialogBackground", HaColors.White)));
        }

        Plot.Refresh();
    }

    /// <summary>A brush of the theme as a plain color, so the renderer never sees an Avalonia type.</summary>
    private HaColor ThemeColor(string key, HaColor fallback)
    {
        return this.TryFindResource(key, ActualThemeVariant, out var resource) && resource is ISolidColorBrush brush
            ? HaColor.FromArgb(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B)
            : fallback;
    }
}
