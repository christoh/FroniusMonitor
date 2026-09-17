namespace De.Hochstaetter.HomeAutomationClient.Controls;

/// <summary>
/// The size a detail page wants the window it opens in to start at. It is set on the page's own root, so that the
/// view which knows how much room its content needs is the one that says so:
/// </summary>
/// <remarks>
/// <code>
/// &lt;ContentPage c:InitialWindowSize.Width="1400" c:InitialWindowSize.Height="900" …&gt;
/// </code>
/// <para>
/// <b>Optional and per view.</b> A dimension that is not set is decided by the content, which is what every page
/// window did before this existed and what a view that says nothing still gets. The two are independent: a page
/// may fix its width, because the widest row of gauges is what its width is for, and still be exactly as tall as
/// what is on it.
/// </para>
/// <para>
/// <b>Initial, not fixed.</b> A page window is the user's to resize from the moment it is up
/// (<see cref="Views.ChildWindow.IsUserResizable"/>), and nothing here is written back or remembered - the next
/// window for that page opens at this size again.
/// </para>
/// <para>
/// <b>The presenter caps the window at a share of the screen</b>, unless the page says otherwise: a dimension the
/// page asks for is cut down to it, and a dimension the content decides is capped by it as well.
/// <see cref="LimitHeightToScreenProperty"/> set to <c>false</c> lifts that cap on the height alone, for a page
/// whose content must not be cut off. The screen itself still bounds the height: Avalonia measures a
/// content-sized window against the platform's maximum, which is the working area, and Windows refuses a
/// resizable window taller than the screen.
/// </para>
/// <para>
/// Only <see cref="Services.Presentation.WindowPresenter"/> reads it, because it is the only presenter that has a
/// window to size. Inside <c>MainView</c> a page fills the view it is put in, and the property is ignored there
/// rather than being an error: the same view runs on every head.
/// </para>
/// </remarks>
public sealed class InitialWindowSize : AvaloniaObject
{
    private InitialWindowSize() { }

    /// <summary>How wide the window opens, or <see cref="double.NaN"/> to let the content decide.</summary>
    public static readonly AttachedProperty<double> WidthProperty =
        AvaloniaProperty.RegisterAttached<InitialWindowSize, Control, double>("Width", double.NaN);

    /// <summary>How tall the window opens, or <see cref="double.NaN"/> to let the content decide.</summary>
    public static readonly AttachedProperty<double> HeightProperty =
        AvaloniaProperty.RegisterAttached<InitialWindowSize, Control, double>("Height", double.NaN);

    /// <summary>
    /// Whether the presenter caps the height at its share of the screen. <c>true</c> by default; <c>false</c> lets
    /// a content-sized height take the whole screen rather than cut the content off.
    /// </summary>
    public static readonly AttachedProperty<bool> LimitHeightToScreenProperty =
        AvaloniaProperty.RegisterAttached<InitialWindowSize, Control, bool>("LimitHeightToScreen", true);

    public static double GetWidth(Control page) => page.GetValue(WidthProperty);

    public static void SetWidth(Control page, double value) => page.SetValue(WidthProperty, value);

    public static double GetHeight(Control page) => page.GetValue(HeightProperty);

    public static void SetHeight(Control page, double value) => page.SetValue(HeightProperty, value);

    public static bool GetLimitHeightToScreen(Control page) => page.GetValue(LimitHeightToScreenProperty);

    public static void SetLimitHeightToScreen(Control page, bool value) => page.SetValue(LimitHeightToScreenProperty, value);
}
