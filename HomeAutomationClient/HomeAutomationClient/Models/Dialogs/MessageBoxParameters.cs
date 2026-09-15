namespace De.Hochstaetter.HomeAutomationClient.Models.Dialogs;

public class MessageBox : DialogParameters
{
    /// <summary>
    /// A message box is the one dialog that gets a modal window: it is the answer to a question the user has just
    /// been asked, so there is nothing else to do until they have answered it.
    /// </summary>
    public override bool IsModalWindow => true;

    public IList<string> Buttons { get; init; } = [Resources.Ok];

    public string? Text { get; init; }

    public IReadOnlyList<string>? ItemList { get; init; }

    public string? TextBelowItemList { get; init; }

    public object? Icon { get; init; }

    public int DefaultButtonIndex { get; init; }
}