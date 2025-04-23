using De.Hochstaetter.Fronius.Models;

namespace De.Hochstaetter.HomeAutomationClient.Models.Dialogs;

public partial class DialogParameters : BindableBase
{
    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ShowCloseBox { get; set; } = true;
}