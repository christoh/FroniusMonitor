using Avalonia.Data;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

/// <summary>
/// A message that shows itself for a few seconds and then fades away, ported from the Toast of FroniusMonitor.
/// Set <see cref="Text"/> to say something; the control clears it again once it has faded.
/// </summary>
/// <remarks>
/// <see cref="Text"/> binds two way by default, exactly as the WPF one does, so the view model property it is
/// bound to ends up empty after the toast is gone and setting the same message twice shows it twice.
/// </remarks>
public partial class Toast : UserControl
{
    private static readonly TimeSpan visibleDuration = TimeSpan.FromSeconds(5);

    /// <summary>Must match the duration of the opacity transition in the XAML.</summary>
    private static readonly TimeSpan fadeDuration = TimeSpan.FromSeconds(1);

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<Toast, string?>(nameof(Text), defaultBindingMode: BindingMode.TwoWay);

    private readonly DispatcherTimer timer = new();
    private bool isFadingOut;

    public Toast()
    {
        InitializeComponent();
        timer.Tick += OnTick;
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty)
        {
            OnTextChanged();
        }
    }

    private void OnTextChanged()
    {
        timer.Stop();
        isFadingOut = false;

        if (string.IsNullOrWhiteSpace(Text))
        {
            Opacity = 0;
            return;
        }

        Opacity = 1;
        timer.Interval = visibleDuration;
        timer.Start();
    }

    /// <summary>
    /// Two ticks per message: the first ends the time it stays up and starts the fade, the second waits for the
    /// fade to finish before the text is cleared - clearing it any earlier would empty the toast mid fade.
    /// </summary>
    private void OnTick(object? sender, EventArgs e)
    {
        timer.Stop();

        if (!isFadingOut)
        {
            isFadingOut = true;
            Opacity = 0;
            timer.Interval = fadeDuration;
            timer.Start();
            return;
        }

        isFadingOut = false;
        Text = null;
    }
}
