namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs
{
    public record DialogQueueItem(
        string Title,
        object Body,
        bool ShowCloseBox,
        string? BusyText
    )
    {
    }
}
