using De.Hochstaetter.Fronius;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.Models.Dialogs;
using De.Hochstaetter.HomeAutomationClient.Services.Presentation;
using De.Hochstaetter.HomeAutomationClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// What every head without windows does - the browser and the phones: one dialog frame, one content host, and the
/// menu bar as the only way to reach a second dialog. This is the behaviour the desktop's windows must not have
/// changed.
/// </summary>
[Collection(AvaloniaCollection.Name)]
public sealed class MainViewPresenterTests
{
    private static MainViewModel Start()
    {
        var presenter = new MainViewPresenter();
        MainViewModel? mainViewModel = null;

        HeadlessAvalonia.Reset(services => services
            .AddSingleton<IDialogPresenter>(presenter)
            .AddSingleton<IPagePresenter>(presenter)
            .AddTransient<TestPage>()
            // The presenter looks the main view model up when it first needs one, so it has to be in the container.
            .AddSingleton(_ => mainViewModel!));

        mainViewModel = HeadlessAvalonia.CreateMainViewModel(presenter, presenter);
        return mainViewModel;
    }

    [Fact]
    public Task A_dialog_is_shown_in_the_frame_of_the_main_view() => HeadlessAvalonia.RunAsync(async () =>
    {
        var mainViewModel = Start();

        var dialog = new TestDialog(new DialogParameters { Title = "First" });
        var shown = dialog.ShowDialogAsync();
        await HeadlessAvalonia.SettleAsync();

        Assert.NotNull(mainViewModel.CurrentDialog);
        Assert.Equal("First", mainViewModel.CurrentDialog.Title);
        Assert.IsType<TestDialogView>(mainViewModel.CurrentDialog.Body);
        Assert.True(mainViewModel.IsDialogVisible);
        Assert.True(mainViewModel.IsModalDialogVisible);
        Assert.False(shown.IsCompleted);

        // And no window was opened for any of it.
        Assert.Empty(HeadlessAvalonia.Windows);

        dialog.Accept();
        await HeadlessAvalonia.SettleAsync();
        Assert.Null(mainViewModel.CurrentDialog);
        Assert.False(mainViewModel.IsDialogVisible);
        Assert.True(await shown);
    });

    [Fact]
    public Task A_nested_dialog_covers_the_one_below_and_gives_it_back_with_its_busy_text() => HeadlessAvalonia.RunAsync(async () =>
    {
        var mainViewModel = Start();

        var first = new TestDialog(new DialogParameters { Title = "First" });
        var firstShown = first.ShowDialogAsync();
        await HeadlessAvalonia.SettleAsync();

        first.SetBusyText("loading");
        Assert.Equal("loading", mainViewModel.DialogBusyText);

        var second = new TestDialog(new DialogParameters { Title = "Second" });
        var secondShown = second.ShowDialogAsync();
        await HeadlessAvalonia.SettleAsync();

        Assert.Equal("Second", mainViewModel.CurrentDialog?.Title);

        // The busy overlay of a nested dialog does not leak into the dialog below it.
        Assert.Null(mainViewModel.DialogBusyText);

        second.Accept();
        await HeadlessAvalonia.SettleAsync();

        Assert.Equal("First", mainViewModel.CurrentDialog?.Title);

        // What was on screen when the nested dialog went up, not what was under the one below it - the bug this
        // pins was restoring from one queue item too far down, so a busy dialog came back idle.
        Assert.Equal("loading", mainViewModel.DialogBusyText);
        Assert.True(await secondShown);

        first.SetBusyText(null);
        first.Accept();
        await firstShown;
    });

    [Fact]
    public Task A_dialog_that_asks_for_the_main_view_gets_it_here_too() => HeadlessAvalonia.RunAsync(async () =>
    {
        var mainViewModel = Start();

        var login = new TestDialog(new DialogParameters { Title = "Login", StaysInMainView = true });
        var shown = login.ShowDialogAsync();
        await HeadlessAvalonia.SettleAsync();

        Assert.Equal("Login", mainViewModel.CurrentDialog?.Title);
        Assert.Empty(HeadlessAvalonia.Windows);

        login.Accept();
        await shown;
    });

    [Fact]
    public Task One_page_per_view_type_is_built_and_shown_again_for_every_device() => HeadlessAvalonia.RunAsync(async () =>
    {
        var mainViewModel = Start();
        var pagePresenter = (IPagePresenter)IoC.GetRegistered<IPagePresenter>();

        pagePresenter.Show<TestPage>("device-a", "Inverter A", page => page.DeviceKey = "device-a");
        var shownPage = mainViewModel.MainViewContent;
        Assert.Equal("device-a", Assert.IsType<TestPage>(shownPage).DeviceKey);

        pagePresenter.Show<TestPage>("device-b", "Inverter B", page => page.DeviceKey = "device-b");

        // The same instance, which is what the container handed out while these views were singletons.
        Assert.Same(shownPage, mainViewModel.MainViewContent);
        Assert.Equal("device-b", Assert.IsType<TestPage>(mainViewModel.MainViewContent).DeviceKey);
        Assert.Empty(HeadlessAvalonia.Windows);
    });

    [Fact]
    public Task The_menu_bar_may_not_open_a_second_dialog_over_the_first() => HeadlessAvalonia.RunAsync(async () =>
    {
        var mainViewModel = Start();

        Assert.True(mainViewModel.CanOpenDialog);
        Assert.True(mainViewModel.SettingsCommand.CanExecute(null));

        var dialog = new TestDialog(new DialogParameters { Title = "Settings" });
        var shown = dialog.ShowDialogAsync();
        await HeadlessAvalonia.SettleAsync();

        Assert.False(mainViewModel.CanOpenDialog);
        Assert.False(mainViewModel.SettingsCommand.CanExecute(null));
        Assert.False(mainViewModel.ShowEnergyChartCommand.CanExecute(null));
        Assert.False(mainViewModel.ChangePasswordCommand.CanExecute(null));
        Assert.False(mainViewModel.LogoutCommand.CanExecute(null));

        // Switching pages stays allowed while a dialog is up; that is what the menu bar is left usable for.
        Assert.True(mainViewModel.ShowDetailsCommand.CanExecute(null));
        Assert.True(mainViewModel.ShowDashboardCommand.CanExecute(null));

        dialog.Accept();
        await HeadlessAvalonia.SettleAsync();

        Assert.True(mainViewModel.SettingsCommand.CanExecute(null));
        Assert.True(mainViewModel.ShowEnergyChartCommand.CanExecute(null));
        await shown;
    });

    [Fact]
    public Task The_dashboard_entry_is_shown_where_a_page_covers_the_dashboard() => HeadlessAvalonia.RunAsync(async () =>
    {
        var mainViewModel = Start();
        Assert.True(mainViewModel.ShowDashboardMenu);
        await Task.CompletedTask;
    });
}
