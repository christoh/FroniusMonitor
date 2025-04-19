namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public class UiDemoViewModel : ViewModelBase
{
    public string Culture => Thread.CurrentThread.CurrentCulture.Name;

    public string ApiUri => IoC.GetRegistered<MainViewModel>().ApiUri;

    public string UiCulture => Thread.CurrentThread.CurrentUICulture.Name;

    public ICommand? ShowDialogCommand => field ??= new RelayCommand(async void () =>
    {
        try
        {
            using var dialog = new MessageBoxViewModel
            (
                new MessageBox
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
                new MessageBox
                {
                    Title = Resources.Error,
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
}
