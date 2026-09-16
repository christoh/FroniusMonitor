using Avalonia.Controls;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// That the headless Avalonia session is up at all. It is the first thing to look at when every other test in the
/// collection fails at once, because then the fault is the session rather than anything it was asked to do.
/// </summary>
[Collection(AvaloniaCollection.Name)]
public sealed class HeadlessSmokeTest
{
    [Fact]
    public Task A_window_can_be_shown() => HeadlessAvalonia.RunAsync(async () =>
    {
        // Takes the windows of whatever ran before down; the session is one for the whole run.
        HeadlessAvalonia.Reset();

        var window = new Window { Width = 100, Height = 100 };
        window.Show();
        await HeadlessAvalonia.SettleAsync();

        Assert.Single(HeadlessAvalonia.Windows);
        window.Close();
    });
}
