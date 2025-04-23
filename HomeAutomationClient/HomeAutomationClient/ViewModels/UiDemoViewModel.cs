using System.Collections.ObjectModel;
using Timer = System.Threading.Timer;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public sealed partial class UiDemoViewModel(IWebClientService webClient) : ViewModelBase, IDisposable
{
    public class KeyedInverter
    {
        public required string Key { get; init; }

        public required Gen24System Inverter { get; init; }
    }

    private Timer? timer;

    public string Culture => Thread.CurrentThread.CurrentCulture.Name;

    public string ApiUri => IoC.GetRegistered<MainViewModel>().ApiUri;

    public string UiCulture => Thread.CurrentThread.CurrentUICulture.Name;

    [ObservableProperty]
    public partial bool ColorAllTicks { get; set; } = true;
    
    [ObservableProperty]
    public partial ObservableCollection<KeyedInverter> Inverters { get; set; } = [];

    public override async Task Initialize()
    {
        BusyText = Resources.Loading;
        await base.Initialize();
        timer = new(TimerElapsed, null, 0, 1000);
    }

    public void Dispose()
    {
        timer?.Dispose();
        GC.SuppressFinalize(this);
    }

    ~UiDemoViewModel()
    {
        Dispose();
    }

    [RelayCommand]
    private void ToggleColorAll()
    {
        ColorAllTicks = !ColorAllTicks;
    }

    private async void TimerElapsed(object? state)
    {
        try
        {
            var inverters = (await webClient.GetGen24Devices().ConfigureAwait(false)).Payload;

            if (inverters == null)
            {
                return;
            }

            foreach (var inverter in Inverters)
            {
                var updatedInverter = inverters.FirstOrDefault(i => i.Key == inverter.Key);

                if (updatedInverter.Key != null)
                {
                    inverter.Inverter?.CopyFrom(updatedInverter.Value);
                    inverters.Remove(updatedInverter);
                }
            }

            foreach (var inverter in inverters)
            {
                Dispatcher.UIThread.InvokeAsync(() => Inverters.Add(new KeyedInverter { Key = inverter.Key, Inverter = inverter.Value }));
            }
        }
        catch
        {
            // ignore
        }
        finally
        {
            BusyText = null;
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
