using System.Threading;
using De.Hochstaetter.HomeAutomationClient.Adapters;
using De.Hochstaetter.HomeAutomationClient.Models.Dialogs;
using De.Hochstaetter.HomeAutomationClient.Views;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public class MessageBoxViewModel(string dialogTitle, MessageBoxParameters parameters)
    : DialogBase<MessageBoxParameters, MessageBoxResult, MessageBoxView>(dialogTitle, parameters)
{
    public void OnButtonClicked(int index)
    {
        if (Parameters == null || index > Parameters.Buttons.Count - 1)
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