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

    /// <summary>
    /// Keeps the dialog inside the main view on a head that would otherwise give it a window of its own. The
    /// login dialog is the one that asks for it: it is what the app shows before there is anything to put a
    /// window beside, and on the desktop a window for it would be a second window over an empty one.
    /// </summary>
    /// <remarks>
    /// Not observable and not read again: where a dialog is shown is settled when it is put up.
    /// </remarks>
    public bool StaysInMainView { get; init; }

    /// <summary>
    /// What tells this dialog from another of the same kind, where a head can show more than one at a time: the
    /// key of the device it is about, so the user gets the settings of each inverter in a window of its own and
    /// never two windows for the same one. <see langword="null"/> for a dialog there is only ever one of.
    /// </summary>
    /// <remarks>
    /// Only the identity, not the device: a dialog asked for a second time while it is already up brings the
    /// window that is there to the front instead of opening another one.
    /// </remarks>
    public object? WindowKey { get; init; }

    /// <summary>
    /// Whether this dialog blocks the windows around it where it gets one of its own. Only a message box does:
    /// it answers a question the user has just been asked, and there is nothing to do until they have. Every
    /// other dialog is a window that may be left standing while the user works in another one.
    /// </summary>
    /// <remarks>
    /// Not the same thing as <see cref="IsModal"/>, which is about the dimming layer of the dialog frame inside
    /// the main view. A dialog is modal there and a window here for different reasons.
    /// </remarks>
    public virtual bool IsModalWindow => false;
}
