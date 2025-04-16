namespace De.Hochstaetter.HomeAutomationClient.Models.Dialogs
{
    public class MessageBoxParameters
    {
        public IList<string> Buttons { get; init; } = [Resources.Ok];

        public string? Text { get; init; }

        public IList<string>? ItemList { get; init; }

        public string? TextBelowItemList { get; init; }
        
        public object? Icon { get; init; }
        
        public int DefaultButtonIndex { get; init; }
    }
}
