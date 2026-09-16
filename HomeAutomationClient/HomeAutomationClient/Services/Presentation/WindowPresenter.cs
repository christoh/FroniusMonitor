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
    /// The window a new one should belong to and open in front of: whichever is active, or the main window. A
    /// message box opened from the settings dialog belongs to that dialog's window, not to the main one.
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

        window.LimitToScreen(MaximumFractionOfScreen);

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
        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
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
        var owner = WindowPresenter.ActiveWindow;

        if (!parameters.IsModalWindow || owner is null)
        {
            if (owner is null)
            {
                window.Show();
            }
            else
            {
                window.Show(owner);
            }

            return;
        }

        // The task of a modal window completes when the window closes, which is what the view model's own
        // cancellation token already tells whoever called ShowDialogAsync. Nothing awaits it, so it is guarded here.
        ViewModelBase.HandleTaskExceptions(() => window.ShowDialog(owner));
    }

    public void Close()
    {
        if (isClosing)
        {
            return;
        }

        isClosing = true;
        parameters.PropertyChanged -= OnParametersChanged;

        if (key is not null)
        {
            presenter.Forget(key);
        }

        window.Close();
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
    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (isClosing)
        {
            // Close() is doing it, so the view model has already said its piece.
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
