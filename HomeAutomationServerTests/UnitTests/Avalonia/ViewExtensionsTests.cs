using Avalonia.Controls;
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
