namespace De.Hochstaetter.HomeAutomationClient.Contracts;

/// <summary>
/// Where a page appears - a detail view of one device. Inside <c>MainView</c> on a browser or a phone, where
/// there is one view at a time, and in a window of its own on the desktop, where there can be one per device.
/// </summary>
public interface IPagePresenter
{
    /// <summary>
    /// Whether showing a page covers the dashboard. True where there is one view at a time, false where every page
    /// gets a window of its own and the dashboard simply stays where it is.
    /// </summary>
    /// <remarks>
    /// What the menu bar asks, so that it offers a way back to the dashboard only where there is something to come
    /// back from - see <c>MainViewModel.ShowDashboardMenu</c>.
    /// </remarks>
    bool ShowsPagesInMainView { get; }

    /// <summary>
    /// Shows the page of one device, or brings the one that is already showing it to the front.
    /// </summary>
    /// <param name="deviceKey">
    /// What tells one device from another, as <c>IKeyedDevice.Key</c> knows it. It is what decides whether a page
    /// is already open for this device, so a head that can show several pages at once shows one per device and
    /// not one per device type.
    /// </param>
    /// <param name="title">The window title, where there is a window. Ignored inside <c>MainView</c>.</param>
    /// <param name="configure">
    /// Hands the page its device, before it is shown. Runs on a page that is being reused as well, so a page that
    /// is brought to the front is looking at the device it was asked for.
    /// </param>
    /// <remarks>
    /// How big the page opens is the page's own to say, with the two optional
    /// <see cref="Controls.InitialWindowSize"/> properties on its root; a page that says nothing is sized by its
    /// content. Only a head that gives pages windows can honor it - inside <c>MainView</c> a page fills the view.
    /// </remarks>
    void Show<TPage>(string deviceKey, string title, Action<TPage> configure) where TPage : Control;

    /// <summary>
    /// Takes every page off screen, for a logout: a page kept open would go on showing the devices of a session
    /// that has ended.
    /// </summary>
    void CloseAll();
}
