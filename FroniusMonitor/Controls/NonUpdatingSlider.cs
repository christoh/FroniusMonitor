namespace De.Hochstaetter.FroniusMonitor.Controls;

public class NonUpdatingSlider : Slider
{
    public event EventHandler<RoutedPropertyChangedEventArgs<double>>? ThumbDragCompleted;
    private static readonly IDataCollectionService dataCollectionService = IoC.TryGetRegistered<IDataCollectionService>()!;

    protected override void OnThumbDragStarted(DragStartedEventArgs e)
    {
        dataCollectionService.SuspendPowerConsumers();
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
            dataCollectionService.ResumePowerConsumers();
        }
    }
}
