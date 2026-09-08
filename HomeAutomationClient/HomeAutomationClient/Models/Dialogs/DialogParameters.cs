using De.Hochstaetter.Fronius.Models;

namespace De.Hochstaetter.HomeAutomationClient.Models.Dialogs;

public partial class DialogParameters : BindableBase
{
    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ShowCloseBox { get; set; } = true;

    /// <summary>
    /// Lets the user move the dialog around the screen by dragging its title bar.
    /// </summary>
    [ObservableProperty]
    public partial bool IsMoveable { get; set; } = true;

    /// <summary>
    /// A modal dialog dims and disables everything behind it. A non-modal one leaves the views operable and
    /// never covers itself with the busy animation.
    /// </summary>
    [ObservableProperty]
    public partial bool IsModal { get; set; } = true;

    /// <summary>
    /// Puts a grip in the bottom right corner of the dialog that the user can drag to resize it. Off by default:
    /// a dialog is as big as what it has to show, and a form dragged wider only grows its whitespace. Worth
    /// turning on where the content is bigger than any sensible default - a table, a log - and the user is the
    /// one who knows how much room to give it.
    /// </summary>
    /// <remarks>
    /// May be switched while the dialog is up, which is how the settings dialog uses it: only its event log tab
    /// has anything to gain from more room, so the tab turns it on while it is the one on screen.
    /// </remarks>
    [ObservableProperty]
    public partial bool IsResizeable { get; set; }
}