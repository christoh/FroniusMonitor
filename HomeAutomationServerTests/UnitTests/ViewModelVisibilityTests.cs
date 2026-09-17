using De.Hochstaetter.HomeAutomationClient.ViewModels;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// What a view model gets from <see cref="ViewModelBase.IsShown"/>: work asked for while its view cannot be seen
/// is done once, when the view is back, and the last piece asked for is the one that is done.
/// </summary>
public sealed class ViewModelVisibilityTests
{
    public sealed class CountingViewModel : ViewModelBase
    {
        public List<string> Done { get; } = [];

        public void Report(string what) => WhenShown(() => Done.Add(what));
    }

    [Fact]
    public void A_shown_view_model_does_the_work_at_once()
    {
        var viewModel = new CountingViewModel();

        viewModel.Report("a");
        viewModel.Report("b");

        Assert.Equal(["a", "b"], viewModel.Done);
    }

    [Fact]
    public void A_hidden_view_model_keeps_the_last_piece_of_work_for_when_it_is_shown()
    {
        var viewModel = new CountingViewModel { IsShown = false };

        viewModel.Report("a");
        viewModel.Report("b");
        Assert.Empty(viewModel.Done);

        viewModel.IsShown = true;
        Assert.Equal(["b"], viewModel.Done);

        // Nothing is kept once it has run.
        viewModel.IsShown = false;
        viewModel.IsShown = true;
        Assert.Equal(["b"], viewModel.Done);
    }

    [Fact]
    public void Hiding_a_view_model_that_has_nothing_pending_does_nothing_when_it_is_shown_again()
    {
        var viewModel = new CountingViewModel();

        viewModel.IsShown = false;
        viewModel.IsShown = true;

        Assert.Empty(viewModel.Done);
    }
}
