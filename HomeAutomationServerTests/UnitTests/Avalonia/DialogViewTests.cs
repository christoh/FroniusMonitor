using Avalonia.Controls;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.Models.Dialogs;
using De.Hochstaetter.HomeAutomationClient.Services.Presentation;
using De.Hochstaetter.HomeAutomationClient.ViewModels;
using De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;
using De.Hochstaetter.HomeAutomationClient.Views.Dialogs;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// <see cref="DialogView"/>, the base every dialog body has had since 2026-10-24 instead of starting its view model's
/// <c>Initialize</c> in a code behind of its own.
/// </summary>
[Collection(AvaloniaCollection.Name)]
public sealed class DialogViewTests
{
    public sealed class CountingViewModel : ViewModelBase
    {
        public int InitializeCount { get; private set; }

        /// <summary>How many <c>DataContextChanged</c> handlers had run when <c>Initialize</c> was started.</summary>
        public int HandlersRunBeforeInitialize { get; private set; } = -1;

        public int HandlersRun { get; set; }

        public override Task Initialize()
        {
            InitializeCount++;
            HandlersRunBeforeInitialize = HandlersRun;
            return Task.CompletedTask;
        }
    }

    public sealed class SubscribingDialogView : DialogView
    {
        public SubscribingDialogView() => DataContextChanged += (_, _) =>
        {
            if (DataContext is CountingViewModel viewModel)
            {
                viewModel.HandlersRun++;
            }
        };
    }

    [Fact]
    public void Every_dialog_body_of_the_client_is_a_DialogView()
    {
        var bodies = typeof(DialogView).Assembly.GetTypes()
            .Where(type => typeof(IDialogControl).IsAssignableFrom(type) && type is { IsClass: true, IsAbstract: false })
            .ToList();

        Assert.Contains(typeof(AboutView), bodies);
        Assert.All(bodies, type => Assert.True(type.IsSubclassOf(typeof(DialogView)), $"{type.Name} is a dialog body but no DialogView"));
    }

    [Fact]
    public Task The_view_model_is_initialized_after_the_body_has_seen_it() => HeadlessAvalonia.RunAsync(() =>
    {
        var viewModel = new CountingViewModel();
        _ = new SubscribingDialogView { DataContext = viewModel };

        Assert.Equal(1, viewModel.InitializeCount);
        Assert.Equal(1, viewModel.HandlersRunBeforeInitialize);
        return Task.CompletedTask;
    });

    [Fact]
    public Task A_dialog_body_is_still_drawn_like_a_UserControl() => HeadlessAvalonia.RunAsync(async () =>
    {
        HeadlessAvalonia.Reset(services => services.AddSingleton<IDialogPresenter>(new WindowPresenter()));
        var body = new AboutView { DataContext = new AboutViewModel(new DialogParameters(), null) };
        var window = new Window { Content = body };
        window.Show();
        await HeadlessAvalonia.SettleAsync();

        // A subclass of a templated control that the theme does not recognize gets no template and shows nothing.
        Assert.NotNull(body.Presenter);
        Assert.True(body.Bounds.Height > 0);
        window.Close();
    });
}
