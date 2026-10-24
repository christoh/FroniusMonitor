using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using De.Hochstaetter.Fronius;
using De.Hochstaetter.HomeAutomationClient.Extensions;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// <c>UseViewModel</c>, which every page calls in its constructor since 2026-10-24 instead of resolving its view model
/// and starting its <c>Initialize</c> by hand.
/// </summary>
[Collection(AvaloniaCollection.Name)]
public sealed class ViewExtensionsTests
{
    [Fact]
    public Task A_page_gets_the_view_model_of_the_container_and_starts_it() => HeadlessAvalonia.RunAsync(() =>
    {
        HeadlessAvalonia.Reset(services => services.AddSingleton<CountingViewModel>());
        var page = new UserControl();

        var viewModel = page.UseViewModel<CountingViewModel>();

        Assert.Same(IoC.TryGetRegistered<CountingViewModel>(), viewModel);
        Assert.Same(viewModel, page.DataContext);
        Assert.Equal(1, viewModel!.InitializeCount);
        return Task.CompletedTask;
    });

    [Fact]
    public Task A_view_follows_the_application_theme_while_it_is_loaded_and_only_once() => HeadlessAvalonia.RunAsync(async () =>
    {
        HeadlessAvalonia.Reset();
        var application = Application.Current!;
        var requested = application.RequestedThemeVariant;
        var calls = 0;
        var view = new UserControl();
        view.FollowApplicationTheme((_, _) => calls++);

        try
        {
            application.RequestedThemeVariant = ThemeVariant.Light;
            Assert.Equal(0, calls);

            var window = new Window { Content = view };
            window.Show();
            await HeadlessAvalonia.SettleAsync();
            application.RequestedThemeVariant = ThemeVariant.Dark;
            Assert.Equal(1, calls);

            // Navigated away, the way the main view swaps its content: the page is kept, but must no longer be called.
            window.Content = new Border();
            await HeadlessAvalonia.SettleAsync();
            application.RequestedThemeVariant = ThemeVariant.Light;
            Assert.Equal(1, calls);

            // And shown again: once per change, not once per time it was loaded.
            window.Content = view;
            await HeadlessAvalonia.SettleAsync();
            application.RequestedThemeVariant = ThemeVariant.Dark;
            Assert.Equal(2, calls);
            window.Close();
        }
        finally
        {
            application.RequestedThemeVariant = requested;
        }
    });

    [Fact]
    public Task Without_a_view_model_in_the_container_the_page_stays_empty() => HeadlessAvalonia.RunAsync(() =>
    {
        // What the designer sees: no container that knows the view model, and a preview that must still render.
        HeadlessAvalonia.Reset();
        var page = new UserControl();

        Assert.Null(page.UseViewModel<CountingViewModel>());
        Assert.Null(page.DataContext);
        return Task.CompletedTask;
    });
}
