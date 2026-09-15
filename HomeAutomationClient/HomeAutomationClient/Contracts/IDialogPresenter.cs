namespace De.Hochstaetter.HomeAutomationClient.Contracts;

/// <summary>
/// Where a dialog appears. There are two of these: the dialog frame inside <c>MainView</c>, which is all a
/// browser or a phone has, and a window of its own, which only the desktop head can do.
/// </summary>
/// <remarks>
/// <see cref="ViewModels.Adapters.DialogBase{TParameters,TResult,TBody}"/> is the only caller. It still owns the
/// waiting - a dialog parks on a cancellation token until it is closed - so an implementation only has to put the
/// body somewhere and take it away again.
/// </remarks>
public interface IDialogPresenter
{
    /// <summary>
    /// Whether a dialog takes the one dialog frame of the main view. True where a second dialog would cover the
    /// first instead of standing beside it, false where every dialog gets a window of its own.
    /// </summary>
    /// <remarks>
    /// What the menu bar asks before it offers to open one - see <c>MainViewModel.CanOpenDialog</c>. The menu bar
    /// is the one thing a modal dialog does not disable, because it has to stay usable for switching pages.
    /// </remarks>
    bool ShowsDialogsInMainView { get; }

    /// <summary>
    /// Makes room for a dialog. The body is handed over afterwards, because creating it assigns its
    /// <c>DataContext</c> and a dialog may set a busy text in its <c>Initialize</c> that has to land somewhere.
    /// </summary>
    /// <returns>
    /// <see langword="null"/> when an equivalent dialog is already on screen and was activated instead of being
    /// opened a second time. The caller then shows nothing and returns its default result.
    /// </returns>
    IDialogPresentation? Create(IDialogBase viewModel, DialogParameters parameters);

    /// <summary>
    /// Takes every dialog off screen, for a logout: what is on them belongs to the session that is ending.
    /// Each dialog is aborted through its own view model, so whoever awaits it is released.
    /// </summary>
    void CloseAll();
}

/// <summary>
/// One dialog that is on screen, or about to be. Handed out by <see cref="IDialogPresenter.Create"/> and used by
/// the dialog's view model for the rest of its life.
/// </summary>
public interface IDialogPresentation
{
    /// <summary>
    /// What the busy animation over this dialog says, or <see langword="null"/> for none. This is where
    /// <c>DialogBase.BusyText</c> goes, so that the animation of a dialog in a window of its own is in that
    /// window rather than over the main view.
    /// </summary>
    string? BusyText { get; set; }

    /// <summary>Hands over the dialog's body. Called once, before <see cref="Open"/>.</summary>
    void SetBody(Control body);

    /// <summary>Puts the dialog on screen.</summary>
    void Open();

    /// <summary>Takes the dialog off screen. Calling it twice is harmless.</summary>
    void Close();
}
