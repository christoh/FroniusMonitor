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

    /// <summary>The explicit size the body declares, while it is lifted off the body - see <see cref="ApplyContentLimits"/>.</summary>
    private double contentWidth = double.NaN;
    private double contentHeight = double.NaN;

    /// <summary>What <see cref="SetInitialSize"/> was asked for, before the body's own size fills in what it left open.</summary>
    private double requestedWidth = double.NaN;
    private double requestedHeight = double.NaN;

    private double initialWidth = double.NaN;
    private double initialHeight = double.NaN;

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
    /// Whether the user may resize the window. A fixed window is exactly as big as what is on it and follows it
    /// when that changes; a resizable one does the same until the user drags an edge, and from then on that
    /// dimension is the user's.
    /// </summary>
    /// <remarks>
    /// This is <see cref="Models.Dialogs.DialogParameters.IsResizeable"/> and may be switched while the window is
    /// open, which is what the inverter settings dialog does for its event log tab. Turning it on also lifts the
    /// maximum width and height the body declares, and an explicit width and height as well - a dialog states
    /// those for the size it wants to have, not for the size the user may drag it to, and a body with an explicit
    /// size would keep it while the window around it grew. The explicit size becomes the size the window opens at
    /// where <see cref="SetInitialSize"/> asked for nothing, and the body's minimum becomes the window's. Turning
    /// it off puts everything back, so the window shrinks to its content again.
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
            ApplyInitialSize();
        }
    }

    /// <summary>
    /// Lifts the maximum width and height the content declares while the window is resizable, and its explicit
    /// width and height with them, and puts them back when it is not. Also run when the content arrives, because
    /// it can arrive after the flag: a dialog may ask for resizing from its <c>Initialize</c>, which runs while
    /// its body is being created.
    /// </summary>
    /// <remarks>
    /// The explicit size has to go for the same reason the maximum does, only more so: a body 1160 wide stays
    /// 1160 wide in a window the user has dragged to 1600, centred, with empty space at both sides - which is
    /// what the two chart dialogs did on the desktop (2026-09-20), because they state their size the way the
    /// dialog frame in <c>MainView</c> wants it, as a <c>Width</c> and a <c>Height</c>. Lifted, the body fills
    /// the window, and the size it declared is what the window opens at (<see cref="ApplyInitialSize"/>). The
    /// body's minimum becomes the window's while it lasts, so the user cannot drag the window smaller than what
    /// the body can lay out.
    /// </remarks>
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
            (contentWidth, contentHeight) = (content.Width, content.Height);
            (content.Width, content.Height) = (double.NaN, double.NaN);
            (MinWidth, MinHeight) = (content.MinWidth, content.MinHeight);
            return;
        }

        (content.MaxWidth, content.MaxHeight) = (contentMaximumWidth, contentMaximumHeight);
        (content.Width, content.Height) = (contentWidth, contentHeight);
        (contentWidth, contentHeight) = (double.NaN, double.NaN);
        (MinWidth, MinHeight) = (0, 0);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == HostedContentProperty && IsUserResizable)
        {
            ApplyContentLimits(change.NewValue as Layoutable, true);
            ApplyInitialSize();
        }
    }

    /// <summary>
    /// Gives the window a size to open at instead of the size of what is on it. <see cref="double.NaN"/> for a
    /// dimension leaves that one to the content - or to the explicit size the body declares, while the window is
    /// resizable and that size is lifted off the body - which is what every window did before this existed; the
    /// two are independent, so a fixed width with a content driven height is a valid combination.
    /// </summary>
    /// <remarks>
    /// Call it after <see cref="LimitToScreen"/> and before the window is shown: the requested size is cut down
    /// to the same maximum, because a view that asks for more than the screen has would otherwise open with its
    /// bottom and its right edge past the edge of it. What the user does with the window afterwards is untouched.
    /// </remarks>
    public void SetInitialSize(double width, double height)
    {
        (requestedWidth, requestedHeight) = (width, height);
        ApplyInitialSize();
    }

    /// <summary>
    /// Works the size the window opens at out of what was asked for and what the body declares, and applies it.
    /// Run again whenever either changes: the body's size is lifted when resizing is switched on, which may be
    /// before <see cref="SetInitialSize"/> and its screen limit, and is put back when it is switched off, when
    /// the window goes back to the size of its content.
    /// </summary>
    private void ApplyInitialSize()
    {
        initialWidth = Requested(double.IsNaN(requestedWidth) ? contentWidth : requestedWidth, MaxWidth);
        initialHeight = Requested(double.IsNaN(requestedHeight) ? contentHeight : requestedHeight, MaxHeight);

        if (!double.IsNaN(initialWidth))
        {
            Width = initialWidth;
        }

        if (!double.IsNaN(initialHeight))
        {
            Height = initialHeight;
        }

        ApplySizeToContent();

        // Zero, a negative number and an infinity are not sizes. They count as "not asked for" rather than as an
        // error: this is one number in the XAML of a view, and there is nobody to report it to from here.
        static double Requested(double requested, double maximum) => double.IsFinite(requested) && requested > 0 ? Math.Min(requested, maximum) : double.NaN;
    }

    /// <summary>
    /// A dimension the view did not fix follows the content, and keeps following it after the window is up: a page
    /// that fills itself once it is loaded - the power flow page has no cards until its view model has heard from
    /// the devices - would otherwise be measured empty and stay that small. Handing the size over to the user is
    /// Avalonia's: <c>Window.HandleResized</c> drops the auto-sizing of the dimension the user drags, that one
    /// only, so a window the user made wider still grows and shrinks with its content in height.
    /// </summary>
    /// <remarks>
    /// A dimension that <see cref="SetInitialSize"/> fixed is not sized to its content at all. Doing both would
    /// have the content win, and the number the view asked for would silently do nothing.
    /// </remarks>
    private void ApplySizeToContent() => SizeToContent =
        (double.IsNaN(initialWidth), double.IsNaN(initialHeight)) switch
        {
            (true, true) => SizeToContent.WidthAndHeight,
            (true, false) => SizeToContent.Width,
            (false, true) => SizeToContent.Height,
            (false, false) => SizeToContent.Manual,
        };

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

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
    /// <param name="limitHeight">
    /// <c>false</c> leaves the height alone, for a page that is sized by its content and would rather take the
    /// whole screen than have that content cut off. The width is capped either way.
    /// </param>
    public void LimitToScreen(double fractionOfScreen, bool limitHeight = true)
    {
        if ((Screens.ScreenFromWindow(this) ?? Screens.Primary ?? Screens.All.FirstOrDefault()) is not { } screen)
        {
            return;
        }

        // The working area is in physical pixels; Width and Height are device independent.
        MaxWidth = screen.WorkingArea.Width / screen.Scaling * fractionOfScreen;

        if (limitHeight)
        {
            MaxHeight = screen.WorkingArea.Height / screen.Scaling * fractionOfScreen;
        }
    }
}
