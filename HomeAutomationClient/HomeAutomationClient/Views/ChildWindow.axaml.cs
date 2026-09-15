using Avalonia.Layout;

namespace De.Hochstaetter.HomeAutomationClient.Views;

/// <summary>
/// The window the desktop head puts a dialog or a detail page in. It is the standard window chrome of the
/// operating system plus the two things the dialog frame of <c>MainView</c> also provides: somewhere to put the
/// body, and a busy animation over it.
/// </summary>
/// <remarks>
/// <see cref="StyledElement.DataContext"/> is the window itself, so that the two bindings in the XAML are
/// compiled ones with no view model in between. Nothing inherits it: every body that goes in here - a dialog
/// view, a detail page - assigns its own <c>DataContext</c> in its constructor.
/// </remarks>
public partial class ChildWindow : Window
{
    public static readonly StyledProperty<object?> HostedContentProperty = AvaloniaProperty.Register<ChildWindow, object?>(nameof(HostedContent));

    public static readonly StyledProperty<string?> BusyTextProperty = AvaloniaProperty.Register<ChildWindow, string?>(nameof(BusyText));

    private double contentMaximumWidth = double.PositiveInfinity;
    private double contentMaximumHeight = double.PositiveInfinity;
    private bool isOpened;

    /// <summary>What the window shows: a dialog body or a detail page.</summary>
    public object? HostedContent
    {
        get => GetValue(HostedContentProperty);
        set => SetValue(HostedContentProperty, value);
    }

    /// <summary>What the busy animation over the content says, or <see langword="null"/> for none.</summary>
    public string? BusyText
    {
        get => GetValue(BusyTextProperty);
        set => SetValue(BusyTextProperty, value);
    }

    public ChildWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>
    /// Whether the user may resize the window. It also decides where the size comes from: a fixed window is
    /// exactly as big as what is on it and follows it when that changes, a resizable one starts there and is the
    /// user's from then on.
    /// </summary>
    /// <remarks>
    /// This is <see cref="Models.Dialogs.DialogParameters.IsResizeable"/> and may be switched while the window is
    /// open, which is what the inverter settings dialog does for its event log tab. Turning it on also lifts the
    /// maximum width and height the body declares - a dialog states those for the size it wants to have, not for
    /// the size the user may drag it to - and turning it off puts them back, so the window shrinks to its content
    /// again.
    /// </remarks>
    public bool IsUserResizable
    {
        get;

        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            ApplyContentLimits(HostedContent as Layoutable, value);
            CanResize = value;
            ApplySizeToContent();
        }
    }

    /// <summary>
    /// Lifts the maximum width and height the content declares while the window is resizable, and puts them back
    /// when it is not. Also run when the content arrives, because it can arrive after the flag: a dialog may ask
    /// for resizing from its <c>Initialize</c>, which runs while its body is being created.
    /// </summary>
    private void ApplyContentLimits(Layoutable? content, bool isResizable)
    {
        if (content is null)
        {
            return;
        }

        if (isResizable)
        {
            (contentMaximumWidth, contentMaximumHeight) = (content.MaxWidth, content.MaxHeight);
            (content.MaxWidth, content.MaxHeight) = (double.PositiveInfinity, double.PositiveInfinity);
            return;
        }

        (content.MaxWidth, content.MaxHeight) = (contentMaximumWidth, contentMaximumHeight);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == HostedContentProperty && IsUserResizable)
        {
            ApplyContentLimits(change.NewValue as Layoutable, true);
        }
    }

    /// <summary>
    /// A window that is not up yet has no size of its own, so the content is what has to give it one - also when
    /// it is going to be resizable. <see cref="OnOpened"/> hands the size over to the user afterwards.
    /// </summary>
    private void ApplySizeToContent() => SizeToContent = IsUserResizable && isOpened ? SizeToContent.Manual : SizeToContent.WidthAndHeight;

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        isOpened = true;
        ApplySizeToContent();

        // Lifted once the window is up: it was only there to keep the window from opening bigger than the screen,
        // and left in place it would stop the user maximizing it.
        MaxWidth = double.PositiveInfinity;
        MaxHeight = double.PositiveInfinity;
    }

    /// <summary>
    /// Brings this window to the user, which is what asking for something that is already open has to do. A
    /// minimized window is restored first: <see cref="Window.Activate"/> alone would make a window the user cannot
    /// see the active one, so the click would look as if it had done nothing at all.
    /// </summary>
    public void Reactivate()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
    }

    /// <summary>
    /// Keeps the window from opening bigger than the screen it opens on. The detail page of an inverter is a wall
    /// of gauges and asks for more room than there is; it scrolls, so the cap costs nothing.
    /// </summary>
    /// <param name="fractionOfScreen">How much of the working area the window may take at most.</param>
    public void LimitToScreen(double fractionOfScreen)
    {
        if ((Screens.ScreenFromWindow(this) ?? Screens.Primary ?? Screens.All.FirstOrDefault()) is not { } screen)
        {
            return;
        }

        // The working area is in physical pixels; Width and Height are device independent.
        MaxWidth = screen.WorkingArea.Width / screen.Scaling * fractionOfScreen;
        MaxHeight = screen.WorkingArea.Height / screen.Scaling * fractionOfScreen;
    }
}
