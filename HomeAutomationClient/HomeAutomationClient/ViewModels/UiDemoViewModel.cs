using System.Timers;
using De.Hochstaetter.HomeAutomationClient.MessageBoxes;
using Timer = System.Threading.Timer;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public partial class UiDemoViewModel(IWebClientService webClient) : ViewModelBase
{
    private Timer timer;

    public string Culture => Thread.CurrentThread.CurrentCulture.Name;

    public string ApiUri => IoC.GetRegistered<MainViewModel>().ApiUri;

    public string UiCulture => Thread.CurrentThread.CurrentUICulture.Name;

    [ObservableProperty]
    private Gen24System? inverter;

    public override async Task Initialize()
    {
        await base.Initialize();
        timer = new Timer(TimerElapsed, null, 0, 1000);
    }

    private async void TimerElapsed(object? state)
    {
        try
        {
            var inverters = await webClient.GetGen24Devices();
            Inverter = inverters.Payload?.Values.First();
        }
        catch
        {
            // ignore
        }
    }

    [RelayCommand]
    private async Task ShowComplexDialog()
    {
        try
        {
            await new MessageBox
            {
                Title = "Test Dialog",
                Buttons = [Resources.Ok, Resources.Cancel],
                Text = "Hello, World! This is a test for a MessageBox that has some longer text items and everything still needs to look good and the text must properly wrap. Please also have a look at the following items.",
                ItemList =
                [
                    "Always prefer composition over inheritance",
                    "If you create an async method, you must ensure that you properly await it. Carefully choose between Task<T> and ValueTask<T> and don't forget to set ConfigureAwait() properly.",
                    "This is an additional bullet item.",
                ],
                TextBelowItemList = $"Did you understand everything? If not, press '{Resources.Cancel}'.",
                Icon = new WarningIcon(),
            }.Show();
        }
        catch
        {
            // ignore
        }
    }

    [RelayCommand]
    private async Task ShowSimpleDialog()
    {
        try
        {
            await new MessageBox
            {
                Title = Resources.Error,
                Text = string.Format(Resources.InverterCommReadError, "Atomkraftwerk 1"),
                Icon = new ErrorIcon(),
            }.Show();

        }
        catch
        {
            // async void must be caught to avoid unhandled exceptions
        }
    }
}
