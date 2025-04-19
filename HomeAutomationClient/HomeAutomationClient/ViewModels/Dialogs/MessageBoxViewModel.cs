using MessageBoxView = De.Hochstaetter.HomeAutomationClient.Views.Dialogs.MessageBoxView;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

public class MessageBoxViewModel(MessageBox parameters)
    : DialogBase<MessageBox, MessageBoxResult, MessageBoxView>(parameters)
{
    public void OnButtonClicked(int index)
    {
        if (index > Parameters.Buttons.Count - 1)
        {
            index = ~0;
        }

        Result = new MessageBoxResult
        {
            Index = index,
            ButtonText = index < 0 ? null : Parameters!.Buttons[index]
        };

        Close();
    }

    public override Task AbortAsync()
    {
        Result = new MessageBoxResult();
        Close();
        return Task.CompletedTask;
    }
}
