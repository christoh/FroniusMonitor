using Avalonia.Controls;
using De.Hochstaetter.Fronius;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.Models.Dialogs;
using De.Hochstaetter.HomeAutomationClient.Services.Presentation;
using De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;
using De.Hochstaetter.HomeAutomationClient.Views;
using Microsoft.Extensions.DependencyInjection;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// What the desktop head does with dialogs and detail pages: a window each, one per device, and the main window
/// left showing the dashboard. See the memory document <c>DialogSystem.Lifecycle</c>.
/// </summary>
[Collection(AvaloniaCollection.Name)]
public sealed class WindowPresenterTests
{
    private static async Task<(WindowPresenter Presenter, Window MainWindow)> StartAsync()
    {
        var presenter = new WindowPresenter();

        HeadlessAvalonia.Reset(services => services
            .AddSingleton<IDialogPresenter>(presenter)
            .AddSingleton<IPagePresenter>(presenter)
            .AddTransient<TestPage>());

        // Something for a new window to belong to and open in front of, as the real main window is.
        var mainWindow = new Window { Title = "main", Width = 400, Height = 300 };
        mainWindow.Show();
        await HeadlessAvalonia.SettleAsync();
        return (presenter, mainWindow);
    }

    private static ChildWindow WindowOf(string title) => HeadlessAvalonia.Windows.OfType<ChildWindow>().Single(window => window.Title == title);

    private static int WindowCount => HeadlessAvalonia.Windows.Count;

    [Fact]
    public Task A_non_modal_dialog_gets_a_window_of_its_own() => HeadlessAvalonia.RunAsync(async () =>
    {
        await StartAsync();

        var dialog = new TestDialog(new DialogParameters { Title = "Settings A", WindowKey = "device-a" });
        var shown = dialog.ShowDialogAsync();
        await HeadlessAvalonia.SettleAsync();

        Assert.Equal(2, WindowCount);
        var window = WindowOf("Settings A");
        Assert.IsType<TestDialogView>(window.HostedContent);
        Assert.Contains("Dialog", window.Classes);
        Assert.False(window.CanResize);
        Assert.False(shown.IsCompleted);

        dialog.Accept();
        await HeadlessAvalonia.SettleAsync();
        Assert.True(await shown);
        Assert.Equal(1, WindowCount);
    });

    [Fact]
    public Task The_busy_text_of_a_dialog_goes_to_its_own_window() => HeadlessAvalonia.RunAsync(async () =>
    {
        await StartAsync();

        var dialog = new TestDialog(new DialogParameters { Title = "Busy", WindowKey = "device-a" });
        var shown = dialog.ShowDialogAsync();
        await HeadlessAvalonia.SettleAsync();

        dialog.SetBusyText("working");
        Assert.Equal("working", WindowOf("Busy").BusyText);
        Assert.Equal("working", dialog.ReadBusyText());

        dialog.SetBusyText(null);
        Assert.Null(WindowOf("Busy").BusyText);

        dialog.Accept();
        await shown;
    });

    [Fact]
    public Task The_same_dialog_for_the_same_device_activates_the_window_that_is_open() => HeadlessAvalonia.RunAsync(async () =>
    {
        await StartAsync();

        var first = new TestDialog(new DialogParameters { Title = "Settings A", WindowKey = "device-a" });
        var firstShown = first.ShowDialogAsync();
        await HeadlessAvalonia.SettleAsync();

        var again = new TestDialog(new DialogParameters { Title = "Settings A", WindowKey = "device-a" });
        var againShown = again.ShowDialogAsync();
        await HeadlessAvalonia.SettleAsync();

        Assert.Equal(2, WindowCount);
        Assert.True(againShown.IsCompleted);
        Assert.False(await againShown);

        // Another device is another window.
        var other = new TestDialog(new DialogParameters { Title = "Settings B", WindowKey = "device-b" });
        var otherShown = other.ShowDialogAsync();
        await HeadlessAvalonia.SettleAsync();
        Assert.Equal(3, WindowCount);

        first.Accept();
        other.Accept();
        await firstShown;
        await otherShown;
    });

    [Fact]
    public Task Asking_again_for_a_minimized_dialog_brings_its_window_back() => HeadlessAvalonia.RunAsync(async () =>
    {
        await StartAsync();

        var dialog = new TestDialog(new DialogParameters { Title = "Minimized", WindowKey = "device-a" });
        var shown = dialog.ShowDialogAsync();
        await HeadlessAvalonia.SettleAsync();

        var window = WindowOf("Minimized");
        window.WindowState = WindowState.Minimized;
        await HeadlessAvalonia.SettleAsync();

        await new TestDialog(new DialogParameters { Title = "Minimized", WindowKey = "device-a" }).ShowDialogAsync();
        await HeadlessAvalonia.SettleAsync();

        Assert.NotEqual(WindowState.Minimized, window.WindowState);
        Assert.Equal(2, WindowCount);

        dialog.Accept();
        await shown;
    });

    [Fact]
    public Task Closing_the_window_aborts_the_dialog_and_releases_its_caller() => HeadlessAvalonia.RunAsync(async () =>
    {
        await StartAsync();

        var dialog = new TestDialog(new DialogParameters { Title = "Closed", WindowKey = "device-a" });
        var shown = dialog.ShowDialogAsync();
        await HeadlessAvalonia.SettleAsync();

        WindowOf("Closed").Close();
        await HeadlessAvalonia.SettleAsync();

        Assert.Equal(1, dialog.AbortCount);
        Assert.True(shown.IsCompleted);
        Assert.False(await shown);
        Assert.Equal(1, WindowCount);

        // And the device may be opened again once its window is gone.
        var again = new TestDialog(new DialogParameters { Title = "Closed", WindowKey = "device-a" });
        var againShown = again.ShowDialogAsync();
        await HeadlessAvalonia.SettleAsync();
        Assert.Equal(2, WindowCount);

        again.Accept();
        await againShown;
    });

    [Fact]
    public Task A_dialog_without_a_close_box_cannot_be_closed_by_its_chrome() => HeadlessAvalonia.RunAsync(async () =>
    {
        await StartAsync();

        var dialog = new TestDialog(new DialogParameters { Title = "No close box", ShowCloseBox = false, WindowKey = "device-a" });
        var shown = dialog.ShowDialogAsync();
        await HeadlessAvalonia.SettleAsync();

        var window = WindowOf("No close box");
        window.Close();
        await HeadlessAvalonia.SettleAsync();

        Assert.Contains(window, HeadlessAvalonia.Windows);
        Assert.Equal(0, dialog.AbortCount);

        // Its own code still closes it.
        dialog.Accept();
        await HeadlessAvalonia.SettleAsync();
        Assert.DoesNotContain(window, HeadlessAvalonia.Windows);
        await shown;
    });

    [Fact]
    public Task Turning_resizing_on_frees_the_maximum_the_body_declares() => HeadlessAvalonia.RunAsync(async () =>
    {
        await StartAsync();

        var dialog = new TestDialog(new DialogParameters { Title = "Resizable", WindowKey = "device-a" });
        var shown = dialog.ShowDialogAsync();
        await HeadlessAvalonia.SettleAsync();
        var window = WindowOf("Resizable");

        // This is what the event log tab of the inverter settings dialog does while it is on screen.
        dialog.Parameters.IsResizeable = true;
        await HeadlessAvalonia.SettleAsync();
        Assert.True(window.CanResize);
        Assert.True(double.IsPositiveInfinity(((TestDialogView)window.HostedContent!).MaxWidth));

        dialog.Parameters.IsResizeable = false;
        await HeadlessAvalonia.SettleAsync();
        Assert.False(window.CanResize);
        Assert.Equal(500, ((TestDialogView)window.HostedContent!).MaxWidth);

        dialog.Accept();
        await shown;
    });

    [Fact]
    public Task A_dialog_that_asks_for_resizing_up_front_gets_it_before_its_body_exists() => HeadlessAvalonia.RunAsync(async () =>
    {
        await StartAsync();

        var dialog = new TestDialog(new DialogParameters { Title = "Wide", IsResizeable = true, WindowKey = "device-a" });
        var shown = dialog.ShowDialogAsync();
        await HeadlessAvalonia.SettleAsync();

        var window = WindowOf("Wide");
        Assert.True(window.CanResize);
        Assert.True(double.IsPositiveInfinity(((TestDialogView)window.HostedContent!).MaxWidth));

        dialog.Accept();
        await shown;
    });

    [Fact]
    public Task A_message_box_is_modal_and_is_never_folded_into_another_one() => HeadlessAvalonia.RunAsync(async () =>
    {
        await StartAsync();

        var first = new MessageBoxViewModel(new MessageBox { Title = "A question", Text = "Really?", Buttons = ["Yes", "No"] }).ShowDialogAsync();
        var second = new MessageBoxViewModel(new MessageBox { Title = "A question", Text = "Again?" }).ShowDialogAsync();
        await HeadlessAvalonia.SettleAsync();

        Assert.Equal(2, HeadlessAvalonia.Windows.OfType<ChildWindow>().Count(window => window.Title == "A question"));
        Assert.False(first.IsCompleted);

        foreach (var box in HeadlessAvalonia.Windows.OfType<ChildWindow>().Where(window => window.Title == "A question").ToList())
        {
            box.Close();
        }

        await HeadlessAvalonia.SettleAsync();
        Assert.True(first.IsCompleted && second.IsCompleted);
        await first;
        await second;
    });

    [Fact]
    public Task Each_device_gets_a_page_window_and_the_same_device_only_one() => HeadlessAvalonia.RunAsync(async () =>
    {
        var (presenter, _) = await StartAsync();
        var pagePresenter = (IPagePresenter)presenter;

        pagePresenter.Show<TestPage>("device-a", "Inverter A", page => page.DeviceKey = "device-a");
        pagePresenter.Show<TestPage>("device-b", "Inverter B", page => page.DeviceKey = "device-b");
        await HeadlessAvalonia.SettleAsync();
        Assert.Equal(3, WindowCount);

        pagePresenter.Show<TestPage>("device-a", "Inverter A", page => page.DeviceKey = "device-a");
        await HeadlessAvalonia.SettleAsync();
        Assert.Equal(3, WindowCount);

        var window = WindowOf("Inverter A");
        Assert.True(window.CanResize);
        Assert.Equal("device-a", Assert.IsType<TestPage>(window.HostedContent).DeviceKey);
        Assert.DoesNotContain("Dialog", window.Classes);

        // Not exactly on top of each other.
        Assert.NotEqual(WindowOf("Inverter B").Position, window.Position);

        // Closed by its chrome, and the device opens again afterwards.
        window.Close();
        await HeadlessAvalonia.SettleAsync();
        Assert.Equal(2, WindowCount);

        pagePresenter.Show<TestPage>("device-a", "Inverter A", page => page.DeviceKey = "device-a");
        await HeadlessAvalonia.SettleAsync();
        Assert.Equal(3, WindowCount);
    });

    [Fact]
    public Task The_dashboard_entry_is_hidden_where_pages_open_in_windows() => HeadlessAvalonia.RunAsync(async () =>
    {
        var (presenter, _) = await StartAsync();
        var mainViewModel = HeadlessAvalonia.CreateMainViewModel(presenter, presenter);

        // The entry brings the dashboard back once a page has taken its place, and here nothing ever covers it.
        Assert.False(mainViewModel.ShowDashboardMenu);

        // And the menu bar may open as many dialogs as the user likes, because each gets a window of its own.
        Assert.True(mainViewModel.CanOpenDialog);
        var dialog = new TestDialog(new DialogParameters { Title = "Settings", WindowKey = "device-a" });
        var shown = dialog.ShowDialogAsync();
        await HeadlessAvalonia.SettleAsync();
        Assert.True(mainViewModel.CanOpenDialog);
        Assert.True(mainViewModel.SettingsCommand.CanExecute(null));
        Assert.True(mainViewModel.ShowEnergyChartCommand.CanExecute(null));
        dialog.Accept();
        await shown;
    });

    [Fact]
    public Task A_logout_takes_every_window_down_and_releases_every_caller() => HeadlessAvalonia.RunAsync(async () =>
    {
        var (presenter, _) = await StartAsync();

        var dialog = new TestDialog(new DialogParameters { Title = "Standing", WindowKey = "device-a" });
        var shown = dialog.ShowDialogAsync();
        ((IPagePresenter)presenter).Show<TestPage>("device-a", "Inverter A", page => page.DeviceKey = "device-a");
        await HeadlessAvalonia.SettleAsync();
        Assert.Equal(3, WindowCount);

        ((IPagePresenter)presenter).CloseAll();
        ((IDialogPresenter)presenter).CloseAll();
        await HeadlessAvalonia.SettleAsync();

        Assert.Equal(1, WindowCount);
        Assert.True(shown.IsCompleted);
        Assert.Equal(1, dialog.AbortCount);
        await shown;
    });
}
