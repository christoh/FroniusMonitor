namespace De.Hochstaetter.FroniusMonitor.Controls;

public abstract class ScalableWindow : Window
{
    public const double ZoomFactor = 1.025;
    protected abstract ScaleTransform Scaler { get; }

    private void Zoom(double zoomFactor)
    {
        Scaler.ScaleX = zoomFactor is 0d ? 1 : Scaler.ScaleX * zoomFactor;
        Scaler.ScaleY = Scaler.ScaleX;
    }

    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        try
        {
            if (e.Handled || Keyboard.IsKeyUp(Key.LeftCtrl) && Keyboard.IsKeyUp(Key.RightCtrl))
            {
                return;
            }

            e.Handled = true;
            Zoom(Math.Pow(ZoomFactor, Math.Sign(e.Delta)));
        }
        finally
        {
            base.OnPreviewMouseWheel(e);
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        try
        {
            if (e.Handled || Keyboard.IsKeyUp(Key.LeftCtrl) && Keyboard.IsKeyUp(Key.RightCtrl))
            {
                return;
            }

            var zoomFactor = e.Key switch
            {
                Key.Add => ZoomFactor,
                Key.OemPlus => ZoomFactor,
                Key.OemMinus => 1 / ZoomFactor,
                Key.Subtract => 1 / ZoomFactor,
                Key.D0 => 0d,
                Key.NumPad0 => 0d,
                _ => double.NaN,
            };

            e.Handled = double.IsFinite(zoomFactor);

            if (e.Handled)
            {
                Zoom(zoomFactor);
            }
        }
        finally
        {
            base.OnPreviewKeyDown(e);
        }
    }
}
