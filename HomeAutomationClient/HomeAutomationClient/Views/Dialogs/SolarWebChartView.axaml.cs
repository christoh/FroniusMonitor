using ScottPlot;
using ScottPlot.Plottables;

namespace De.Hochstaetter.HomeAutomationClient.Views.Dialogs;

/// <summary>
/// The body of the Solar.web chart dialog. What is in the code behind is exactly the two things the interaction rule
/// leaves there: drawing the view model's <see cref="SolarWebChartModel"/> with the charting library, and reading
/// the theme's colours for it - both need the UI framework, and neither decides anything.
/// </summary>
public partial class SolarWebChartView : UserControl, IDialogControl
{
    /// <summary>How far the tooltip box stands off the pointer, so that the pointer never covers its first line.</summary>
    private const double TooltipOffset = 16;

    private SolarWebChartViewModel? viewModel;

    /// <summary>The line at the hovered instant or column, while there is one. Dropped with the plot on every render.</summary>
    private VerticalLine? crosshair;

    public SolarWebChartView()
    {
        // Before the AvaPlot is built: a plot takes its font at construction, and it has to be the app's Inter,
        // not whatever Skia finds on the platform - see InterFontResolver.
        InterFontResolver.Register();
        InitializeComponent();

        // Zoom and pan are off, as in the price chart: a day is a day, and dragging it about only loses the labels.
        Plot.UserInputProcessor.IsEnabled = false;

        // The tooltip, as Solar.web has it. Pointer events and the pixel-to-coordinate mapping are the charting
        // library's and the framework's, so this is where they are handled; what the tooltip says for the instant
        // or the column found is the model's, and the view model holds it (Tooltip) for the overlay to bind to.
        Plot.PointerMoved += OnPointerMoved;
        Plot.PointerExited += (_, _) => HideTooltip();

        DataContextChanged += OnDataContextChanged;
        ActualThemeVariantChanged += (_, _) => Render();
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (viewModel?.ChartModel is not { } model || Plot.Plot.RenderManager.LastRender.Count == 0)
        {
            HideTooltip();
            return;
        }

        // ScottPlot's pixels are device pixels; Avalonia's positions are device independent.
        var position = e.GetPosition(Plot);
        var pixel = new Pixel((float)(position.X * Plot.DisplayScale), (float)(position.Y * Plot.DisplayScale));

        if (!Plot.Plot.RenderManager.LastRender.DataRect.Contains(pixel))
        {
            HideTooltip();
            return;
        }

        var x = Plot.Plot.GetCoordinates(pixel, Plot.Plot.Axes.Bottom, Plot.Plot.Axes.Left).X;

        if (!double.IsFinite(x))
        {
            // An axis that has not been laid out yet answers with NaN; there is nothing to say for it.
            HideTooltip();
            return;
        }

        var tooltip = model.IsCategorical ? model.TooltipForCategory((int)Math.Round(x)) : model.TooltipForTime(DateTime.FromOADate(x));
        viewModel.Tooltip = tooltip;

        if (tooltip == null)
        {
            RemoveCrosshair();
            return;
        }

        var crosshairX = tooltip.Category ?? tooltip.Time!.Value.ToOADate();

        if (crosshair == null)
        {
            crosshair = Plot.Plot.Add.VerticalLine(crosshairX);
            crosshair.LineWidth = 1;
            crosshair.LinePattern = LinePattern.Dotted;
            crosshair.LineColor = ChartTheme.ToColor(ChartTheme.PaletteOf(this).Foreground).WithAlpha(0.6);
        }

        crosshair.X = crosshairX;
        Plot.Refresh();
        PlaceTooltip(position);
    }

    /// <summary>
    /// Puts the box beside the pointer, to its right and below it, and to its left or above it where it would run
    /// past the plot's edge. Measured first, because its size is what the box says at this instant.
    /// </summary>
    private void PlaceTooltip(Point position)
    {
        // The desired size includes the margin, which is where the box stood at the last pointer move; cleared
        // first, so that the size is the box's own.
        TooltipHost.Margin = default;
        TooltipHost.Measure(Size.Infinity);
        var size = TooltipHost.DesiredSize;
        var left = position.X + TooltipOffset + size.Width > Plot.Bounds.Width ? Math.Max(0, position.X - TooltipOffset - size.Width) : position.X + TooltipOffset;
        var top = position.Y + TooltipOffset + size.Height > Plot.Bounds.Height ? Math.Max(0, position.Y - TooltipOffset - size.Height) : position.Y + TooltipOffset;
        TooltipHost.Margin = new Thickness(left, top, 0, 0);
    }

    private void HideTooltip()
    {
        if (viewModel != null)
        {
            viewModel.Tooltip = null;
        }

        RemoveCrosshair();
    }

    private void RemoveCrosshair()
    {
        if (crosshair == null)
        {
            return;
        }

        Plot.Plot.Remove(crosshair);
        crosshair = null;
        Plot.Refresh();
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
        // The crosshair goes with everything else; the next pointer move draws a new one.
        crosshair = null;
        Plot.Reset();

        if (viewModel?.ChartModel is { } model)
        {
            SolarWebChartRenderer.Render(Plot.Plot, model, ChartTheme.PaletteOf(this));
        }

        Plot.Refresh();
    }
}
