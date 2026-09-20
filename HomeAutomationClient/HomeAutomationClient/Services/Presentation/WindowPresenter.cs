namespace De.Hochstaetter.HomeAutomationClient.Services.Presentation;

/// <summary>
/// Gives every dialog and every detail page a window of its own, which is what the desktop head can do and the
/// browser and the phones cannot. A window brings the chrome of the operating system with it, so the title bar,
/// the close box, moving and resizing that <c>MainView</c> has to draw itself are the real ones here.
/// </summary>
/// <remarks>
/// <para>
/// What stays in the main view: the dashboard, and the login dialog, which asks for
/// <see cref="DialogParameters.StaysInMainView"/> - there is nothing to put a window beside while nobody is
/// logged in. Those go to <see cref="MainViewPresenter"/>, which this one keeps for the purpose.
/// </para>
/// <para>
/// Only a message box is modal, because a message box is the one dialog that answers a question the user has
/// just been asked. Everything else is a window that can be left standing: the settings of one inverter beside
/// the settings of another, a detail page beside the dashboard. Opening the same thing twice is what
/// <see cref="DialogParameters.WindowKey"/> and the device key of a page prevent - the window that is there
/// already comes to the front instead.
/// </para>
/// </remarks>
public sealed class WindowPresenter : IDialogPresenter, IPagePresenter
{
    /// <summary>How much of the screen a window may take when it opens; it can be maximized afterwards.</summary>
    private const double MaximumFractionOfScreen = 0.9;

    /// <summary>How far each further page window is offset, so two of them are not exactly on top of each other.</summary>
    private const int CascadeStep = 32;

    private const int MaximumCascadeSteps = 8;

    private readonly MainViewPresenter mainViewPresenter = new();
    private readonly Dictionary<object, WindowDialogPresentation> dialogs = [];
    private readonly Dictionary<object, ChildWindow> pages = [];

    /// <summary>
    /// The window a modal one belongs to and opens in front of: whichever is active, or the main window. A
    /// message box opened from the settings dialog belongs to that dialog's window, not to the main one. Nothing
    /// else has an owner: a dialog or a page that is left standing is a window of the app in its own right.
    /// </summary>
    internal static Window? ActiveWindow => Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
        ? desktop.Windows.FirstOrDefault(window => window.IsActive) ?? desktop.MainWindow
        : null;

    /// <summary>
    /// Never: a page opens in a window of its own, so the dashboard stays in the main view the whole time and is
    /// never covered by one.
    /// </summary>
    public bool ShowsPagesInMainView => false;

    /// <summary>
    /// Never: every dialog stands in a window beside the others, which is the point of this presenter. The login
    /// is the exception it hands to <see cref="MainViewPresenter"/>, and the menu bar is not on screen for it.
    /// </summary>
    public bool ShowsDialogsInMainView => false;

    public IDialogPresentation? Create(IDialogBase viewModel, DialogParameters parameters)
    {
        if (parameters.StaysInMainView)
        {
            return mainViewPresenter.Create(viewModel, parameters);
        }

        // A message box is never reused: it answers one question, and one over another is normal.
        if (parameters.IsModalWindow)
        {
            return new WindowDialogPresentation(this, viewModel, parameters, key: null);
        }

        var key = (viewModel.GetType(), parameters.WindowKey);

        if (dialogs.TryGetValue(key, out var openDialog))
        {
            openDialog.Activate();
            return null;
        }

        var presentation = new WindowDialogPresentation(this, viewModel, parameters, key);
        dialogs[key] = presentation;
        return presentation;
    }

    void IDialogPresenter.CloseAll()
    {
        // Through the view models, not through the windows: whoever awaits ShowDialogAsync has to be released,
        // and AbortAsync is where each dialog decides what that means. The copy is because closing removes the entry.
        foreach (var dialog in dialogs.Values.ToList())
        {
            dialog.Abort();
        }
    }

    public void Show<TPage>(string deviceKey, string title, Action<TPage> configure) where TPage : Control
    {
        var key = (typeof(TPage), deviceKey);

        if (pages.TryGetValue(key, out var openWindow))
        {
            // The same device, so the page is looking at the right thing already. It runs again anyway, because
            // the caller may hold a fresher object of that device than the page was handed last time.
            configure((TPage)openWindow.HostedContent!);
            openWindow.Reactivate();
            return;
        }

        var page = IoC.Get<TPage>();
        configure(page);

        var window = new ChildWindow
        {
            Title = title,
            HostedContent = page,
            // A detail page scrolls and is worth every pixel, so it is the user's to size from the start.
            IsUserResizable = true,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };

        // The screen caps the window, unless the page would rather be as tall as its content whatever the screen.
        window.LimitToScreen(MaximumFractionOfScreen, InitialWindowSize.GetLimitHeightToScreen(page));

        // What the page asks for in its own XAML, if it asks at all; NaN for either dimension leaves that one to
        // the content. Read here and not in the window, because a window has no idea that what is on it is a
        // page - it is the presenter that puts the two together. After LimitToScreen, which is the cap it obeys.
        window.SetInitialSize(InitialWindowSize.GetWidth(page), InitialWindowSize.GetHeight(page));

        window.Closed += (_, _) => pages.Remove(key);
        Cascade(window, pages.Count);
        pages[key] = window;

        // No owner: a detail page is a window of the app in its own right, not something that has to stay in
        // front of the dashboard. It is closed with the main window - see App.OnFrameworkInitializationCompleted.
        window.Show();
    }

    void IPagePresenter.CloseAll()
    {
        foreach (var window in pages.Values.ToList())
        {
            window.Close();
        }

        pages.Clear();
    }

    internal void Forget(object key) => dialogs.Remove(key);

    /// <summary>
    /// Offsets each further window, so the page of the second device does not land exactly on the first one.
    /// </summary>
    private static void Cascade(Window window, int openWindowCount)
    {
        if (openWindowCount <= 0)
        {
            return;
        }

        var offset = CascadeStep * Math.Min(openWindowCount, MaximumCascadeSteps);

        void Offset(object? sender, EventArgs e)
        {
            window.Opened -= Offset;
            var scaling = window.Screens.ScreenFromWindow(window)?.Scaling ?? 1;
            window.Position = new PixelPoint(window.Position.X + (int)(offset * scaling), window.Position.Y + (int)(offset * scaling));
        }

        window.Opened += Offset;
    }
}

/// <summary>
/// One dialog in a window of its own.
/// </summary>
internal sealed class WindowDialogPresentation : IDialogPresentation
{
    private readonly WindowPresenter presenter;
    private readonly IDialogBase viewModel;
    private readonly DialogParameters parameters;
    private readonly object? key;
    private readonly ChildWindow window = new();
    private bool isClosing;

    public WindowDialogPresentation(WindowPresenter presenter, IDialogBase viewModel, DialogParameters parameters, object? key)
    {
        this.presenter = presenter;
        this.viewModel = viewModel;
        this.parameters = parameters;
        this.key = key;

        window.Title = parameters.Title;
        window.Classes.Add("Dialog");
        // Only a modal window has an owner to be centered on; CenterOwner without one places the window nowhere.
        window.WindowStartupLocation = parameters.IsModalWindow ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen;
        window.Closing += OnClosing;
        parameters.PropertyChanged += OnParametersChanged;
    }

    public string? BusyText
    {
        get => window.BusyText;
        set => window.BusyText = value;
    }

    public void SetBody(Control body)
    {
        window.HostedContent = body;
        window.IsUserResizable = parameters.IsResizeable;
    }

    public void Open()
    {
        window.LimitToScreen(1);

        // A dialog body may say how big it opens, the way a page does - the power flow page does, because it has
        // no size of its own to be content sized by. A body that says nothing, which is every form, stays as big
        // as what is on it.
        if (window.HostedContent is Control body)
        {
            window.SetInitialSize(InitialWindowSize.GetWidth(body), InitialWindowSize.GetHeight(body));
        }

        // No owner for a window that is left standing, the same as a page window: an owned window sits in front of
        // its owner for good and is minimized with it, and the settings of an inverter are not a satellite of the
        // dashboard. The main window still takes it down when it goes, through ApplicationShutdown - see OnClosing.
        var owner = parameters.IsModalWindow ? WindowPresenter.ActiveWindow : null;

        if (owner is null)
        {
            window.Show();
            return;
        }

        // The task of a modal window completes when the window closes, which is what the view model's own
        // cancellation token already tells whoever called ShowDialogAsync. Nothing awaits it, so it is guarded here.
        ViewModelBase.HandleTaskExceptions(() => window.ShowDialog(owner));
    }

    public void Close()
    {
        if (Detach())
        {
            window.Close();
        }
    }

    /// <summary>
    /// Takes this presentation out of the presenter and off its parameters, and says whether it was still there.
    /// Both ways out do exactly this - <see cref="Close"/>, and a window closing for a reason the dialog is not
    /// allowed to refuse - and only the first of the two also has to close the window.
    /// </summary>
    private bool Detach()
    {
        if (isClosing)
        {
            return false;
        }

        isClosing = true;
        parameters.PropertyChanged -= OnParametersChanged;

        if (key is not null)
        {
            presenter.Forget(key);
        }

        return true;
    }

    public void Activate() => window.Reactivate();

    /// <summary>
    /// Ends the dialog the way its close box would, for a logout. It goes through the view model, so whoever
    /// awaits <c>ShowDialogAsync</c> gets a result, and the dialog decides what an abort means for it.
    /// </summary>
    public void Abort() => ViewModelBase.HandleTaskExceptions(viewModel.AbortAsync);

    /// <summary>
    /// A dialog may change how its frame behaves while it is up. The one of those a window can follow is whether
    /// it may be resized - the inverter settings dialog turns that on for its event log tab.
    /// </summary>
    private void OnParametersChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DialogParameters.IsResizeable):
                window.IsUserResizable = parameters.IsResizeable;
                break;

            case nameof(DialogParameters.Title):
                window.Title = parameters.Title;
                break;
        }
    }

    /// <summary>
    /// The close box of the window chrome, which does what the close box of the dialog frame does: it asks the
    /// view model to abort, and that is what sets a result and closes. The close itself is cancelled and left to
    /// the view model, because a window closed from under a dialog would leave whoever awaits it waiting forever.
    /// </summary>
    /// <remarks>
    /// <b>Only the user closing this one window may be refused.</b> A dialog is not entitled to veto the
    /// application shutting down, the operating system going, or the window it belongs to closing - and cancelling
    /// here vetoes exactly that, because a close any window cancels is a close that does not happen. That is what
    /// kept the app running with nothing on screen: closing the main window shuts the app down
    /// (<c>ShutdownMode.OnMainWindowClose</c>), one open dialog cancelled it, and the process was left alive after
    /// its last window had gone. So for those reasons the dialog is taken down instead of being asked, through the
    /// view model as everywhere else, and the window is left to close.
    /// </remarks>
    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (isClosing)
        {
            // Close() is doing it, so the view model has already said its piece.
            return;
        }

        // Undefined is deliberately not in this list: a platform that does not say why is the user closing the
        // window as far as anything here can tell, and that is the case the dialog does get a say in.
        if (e.CloseReason is WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown or WindowCloseReason.OwnerWindowClosing)
        {
            Detach();
            Abort();
            return;
        }

        e.Cancel = true;

        // A dialog without a close box cannot be dismissed by its chrome either.
        if (parameters.ShowCloseBox)
        {
            Abort();
        }
    }
}
