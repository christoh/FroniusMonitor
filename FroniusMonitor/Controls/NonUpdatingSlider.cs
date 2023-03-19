namespace De.Hochstaetter.FroniusMonitor.Controls;

public class NonUpdatingSlider : Slider
{
    public event EventHandler<RoutedPropertyChangedEventArgs<double>>? ThumbDragCompleted;
    private static readonly ISolarSystemService solarSystemService = IoC.TryGetRegistered<ISolarSystemService>()!;

    protected override void OnThumbDragStarted(DragStartedEventArgs e)
    {
        solarSystemService.SuspendPowerConsumers();
        base.OnThumbDragStarted(e);
    }

    protected override async void OnThumbDragCompleted(DragCompletedEventArgs e)
    {
        try
        {
            base.OnThumbDragCompleted(e);
            ThumbDragCompleted?.Invoke(this, new RoutedPropertyChangedEventArgs<double>(double.NaN, Value));
            await Task.Delay(1000).ConfigureAwait(false);
        }
        finally
        {
            solarSystemService.ResumePowerConsumers();
        }
    }
}
