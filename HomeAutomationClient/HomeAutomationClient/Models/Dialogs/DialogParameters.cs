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
}