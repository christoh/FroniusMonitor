using De.Hochstaetter.HomeAutomationClient.Assets.Images;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ICache? cache = IoC.TryGetRegistered<ICache>();

    public string? ApiUri => cache?.Get<string>("apiUri");

    public string Culture => Thread.CurrentThread.CurrentCulture.Name;

    public string UiCulture => Thread.CurrentThread.CurrentUICulture.Name;

    [ObservableProperty] public partial bool IsDialogVisible { get; set; }

    [ObservableProperty] public partial object? DialogContent { get; set; }

    [ObservableProperty] public partial string? TitleText { get; set; }

    public ICommand? ShowDialogCommand => field ??= new RelayCommand(async void () =>
    {
        try
        {
            using var dialog = new MessageBoxViewModel
            (
                "Test Dialog",
                new MessageBoxParameters
                {
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
                }
            );

            var result = await dialog.ShowDialogAsync().ConfigureAwait(false);
        }
        catch
        {
            // ignore
        }
    });

    public ICommand? ShowSimpleDialogCommand => field ??= new RelayCommand(async void () =>
    {
        try
        {
            using var dialog = new MessageBoxViewModel
            (
                Resources.Error,
                new MessageBoxParameters
                {
                    Text = string.Format(Resources.InverterCommReadError, "Atomkraftwerk 1"),
                    Icon = new ErrorIcon(),
                }
            );

            var result = await dialog.ShowDialogAsync().ConfigureAwait(false);
        }
        catch
        {
            // async void must be caught to avoid unhandled exceptions
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
            // async void must be caught to avoid unhandled exceptions
        }
    });
}
