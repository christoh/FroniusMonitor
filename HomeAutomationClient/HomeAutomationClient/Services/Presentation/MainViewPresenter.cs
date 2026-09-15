namespace De.Hochstaetter.HomeAutomationClient.Services.Presentation;

/// <summary>
/// Shows dialogs and pages inside <c>MainView</c>: the dialog frame with its own title bar for a dialog, and the
/// one content host for a page. This is what a browser and a phone have, because neither has a second window -
/// see [[DialogSystem.Lifecycle]]. The desktop head uses <see cref="WindowPresenter"/> instead.
/// </summary>
/// <remarks>
/// <see cref="MainViewModel"/> is resolved when it is first needed rather than injected, because it asks for
/// <see cref="IPagePresenter"/> itself and a constructor of each asking for the other cannot be built.
/// </remarks>
public sealed class MainViewPresenter : IDialogPresenter, IPagePresenter
{
    /// <summary>
    /// One page per view type, which is what the container handed out while these views were singletons: a page
    /// left and returned to is the same instance, with the gauge groups the user switched on still switched on.
    /// </summary>
    private readonly Dictionary<Type, Control> pages = [];

    private MainViewModel MainViewModel => field ??= IoC.GetRegistered<MainViewModel>();

    /// <summary>There is one view at a time here, so a page covers the dashboard until the user goes back.</summary>
    public bool ShowsPagesInMainView => true;

    /// <summary>And one dialog frame, so a second dialog opened from the menu would cover the first.</summary>
    public bool ShowsDialogsInMainView => true;

    public IDialogPresentation? Create(IDialogBase viewModel, DialogParameters parameters)
    {
        return new MainViewDialogPresentation(MainViewModel, parameters);
    }

    /// <summary>
    /// Nothing to do: a dialog here is inside the main view, and a logout replaces what the main view shows
    /// anyway. Only a head with windows of its own has dialogs that would outlive the session.
    /// </summary>
    void IDialogPresenter.CloseAll()
    {
    }

    public void Show<TPage>(string deviceKey, string title, Action<TPage> configure) where TPage : Control
    {
        if (!pages.TryGetValue(typeof(TPage), out var page))
        {
            pages[typeof(TPage)] = page = IoC.Get<TPage>();
        }

        configure((TPage)page);
        MainViewModel.MainViewContent = page;
    }

    void IPagePresenter.CloseAll()
    {
        // The page is whatever the main view shows, and MainViewModel.Logout clears that itself. The cached
        // instances are dropped so a new session builds its pages from the devices it is told about.
        pages.Clear();
    }
}

/// <summary>
/// One dialog in the dialog frame of <c>MainView</c>. The queue, the busy text handover and the restoring on
/// close are what <c>DialogBase</c> did before there was more than one place to put a dialog; nothing about them
/// has changed.
/// </summary>
internal sealed class MainViewDialogPresentation : IDialogPresentation
{
    private readonly MainViewModel mainViewModel;
    private readonly DialogParameters parameters;
    private readonly string? busyTextBelow;
    private Control? body;

    public MainViewDialogPresentation(MainViewModel mainViewModel, DialogParameters parameters)
    {
        this.mainViewModel = mainViewModel;
        this.parameters = parameters;
        mainViewModel.DialogQueue.Push(mainViewModel.CurrentDialog);

        // Take the busy text of whatever was on screen and clear it *before* the body is created. Creating the
        // body assigns its DataContext, which starts Initialize, and a dialog that sets a busy text there is on
        // the UI thread and gets that far synchronously. With the two the other way round the new busy text was
        // read into the item and the clear then wiped it, so the dialog came up with no busy animation and the
        // item held the wrong text to restore.
        busyTextBelow = BusyText;
        BusyText = null;
    }

    public string? BusyText
    {
        get => mainViewModel.DialogBusyText;
        set => mainViewModel.DialogBusyText = value;
    }

    public void SetBody(Control body) => this.body = body;

    public void Open() => mainViewModel.CurrentDialog = new DialogQueueItem(parameters.Title, body!, parameters, busyTextBelow);

    public void Close()
    {
        if (isClosed)
        {
            return;
        }

        isClosed = true;
        mainViewModel.CurrentDialog = mainViewModel.DialogQueue.TryPop(out var previousDialog) ? previousDialog : null;

        // What was on screen when this dialog went up, which is what this presentation took away and is therefore
        // the only one that can put back. Reading it off the item that comes back instead - which is what
        // DialogBase did until the presenters were split out - restores the busy text of the dialog *below* the
        // one being uncovered, so a dialog that was busy when a message box opened over it came back idle.
        BusyText = busyTextBelow;
    }

    private bool isClosed;
}
