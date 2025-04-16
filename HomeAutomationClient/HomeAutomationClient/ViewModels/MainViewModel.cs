using System.Threading;
using De.Hochstaetter.HomeAutomationClient.Adapters;
using De.Hochstaetter.HomeAutomationClient.Models.Dialogs;
using De.Hochstaetter.HomeAutomationClient.Views;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ICache? cache = IoC.TryGetRegistered<ICache>();

    public string? ApiUri => cache?.Get<string>("apiUri");

    public string Culture => Thread.CurrentThread.CurrentCulture.Name;

    public string UiCulture => Thread.CurrentThread.CurrentUICulture.Name;

    [ObservableProperty]
    private bool isDialogVisible;

    [ObservableProperty]
    private object? dialogContent;

    [ObservableProperty]
    private string? titleText;

    public ICommand? ShowDialogCommand => field ??= new RelayCommand(async void () =>
    {
        try
        {
            using var dialog = new MessageBoxViewModel
            (
                "Test Dialog",
                new MessageBoxParameters
                {
                    Buttons = [Resources.Cancel, Resources.Ok],
                    Text = "Hello, Dialog!",
                }
            );

            var result = await dialog.ShowDialogAsync().ConfigureAwait(false);
        }
        catch
        {
            // ignore
        }
    });

    public ICommand? DialogClosedCommand => field ??= new RelayCommand(async void () =>
    {
        try
        {
            if (DialogContent is IDialogControl { DataContext: IDialogBase dialogBase })
            {
                await dialogBase.AbortAsync().ConfigureAwait(false);
                return;
            }

            IsDialogVisible = false;
        }
        catch
        {
            // ignore
        }
    });
}
