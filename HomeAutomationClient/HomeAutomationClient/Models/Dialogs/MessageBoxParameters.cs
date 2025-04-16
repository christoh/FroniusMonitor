namespace De.Hochstaetter.HomeAutomationClient.Models.Dialogs
{
    public class MessageBoxParameters
    {
        public IList<string> Buttons { get; set; } = [Resources.Ok];

        public string? Text { get; set; }

        public IList<string>? ItemList { get; init; }

        public string? TextBelowItemList { get; set; }
    }
}
