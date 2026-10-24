using Avalonia.Controls;
using Avalonia.VisualTree;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.Models.Dialogs;
using De.Hochstaetter.HomeAutomationClient.Services.Presentation;
using De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;
using De.Hochstaetter.HomeAutomationClient.Views;
using De.Hochstaetter.HomeAutomationClient.Views.Dialogs;
using Loc = De.Hochstaetter.Fronius.Localization.Resources;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The About box in a window of its own, as the desktop shows it: this app first, the server below it with the same
/// lines, and this app alone where the server did not answer.
/// </summary>
[Collection(AvaloniaCollection.Name)]
public sealed class AboutViewTests
{
    private static readonly WebApiInfo Server = new()
    {
        ProductName = "Home Automation Server Arm64",
        Version = "9.8.7-beta",
        Manufacturer = "Test Manufacturer",
        OsArch = "Arm64",
        OsVersion = "Test OS 1.0",
        DotNetVersion = ".NET 10.0.99",
        CommitTime = new DateTimeOffset(2026, 10, 20, 10, 5, 0, TimeSpan.Zero),
        Branch = "dev",
        CommitId = "0123456789abcdef",
        Configuration = "Release",
    };

    private static async Task<(AboutViewModel Dialog, Task<bool> Shown, ChildWindow Window)> ShowAsync(WebApiInfo? server)
    {
        var presenter = new WindowPresenter();
        HeadlessAvalonia.Reset(services => services.AddSingleton<IDialogPresenter>(presenter).AddSingleton<IPagePresenter>(presenter));

        var dialog = new AboutViewModel(new DialogParameters { Title = Loc.About }, server);
        var shown = dialog.ShowDialogAsync();
        await HeadlessAvalonia.SettleAsync();

        var window = HeadlessAvalonia.Windows.OfType<ChildWindow>().Single(w => w.Title == Loc.About);
        Assert.IsType<AboutView>(window.HostedContent);
        return (dialog, shown, window);
    }

    /// <summary>Every line of text the user can see, runs joined the way they are drawn.</summary>
    private static List<string> LinesOf(Window window) => window.GetVisualDescendants()
        .OfType<TextBlock>()
        .Where(block => block.IsEffectivelyVisible)
        .Select(block => (block.Inlines is { Count: > 0 } inlines ? inlines.Text : block.Text) ?? string.Empty)
        .ToList();

    [Fact]
    public Task This_app_and_the_server_are_shown_with_the_same_lines() => HeadlessAvalonia.RunAsync(async () =>
    {
        var (dialog, shown, window) = await ShowAsync(Server);
        var lines = LinesOf(window);
        var client = dialog.Client;

        Assert.Contains(Loc.AppName, lines);
        Assert.Contains(client.BuildInfo, lines);
        Assert.Contains(lines, line => line.Contains(client.Version!) && line.Contains("UTC"));

        Assert.Contains(Server.ProductName, lines);
        Assert.Contains("Release:dev:0123456789abcdef", lines);
        Assert.Contains(lines, line => line.Contains("9.8.7-beta") && line.Contains("UTC"));
        Assert.Contains(lines, line => line.Contains(".NET 10.0.99") && line.Contains("Test OS 1.0") && line.Contains("(Arm64)"));
        Assert.Contains(lines, line => line.Contains("Test Manufacturer"));

        // The app above the server, as the dialog is read.
        Assert.True(lines.IndexOf(Loc.AppName) < lines.IndexOf(Server.ProductName!));

        dialog.CloseDialogCommand.Execute(null);
        await HeadlessAvalonia.SettleAsync();
        Assert.True(await shown);
        Assert.DoesNotContain(window, HeadlessAvalonia.Windows);
    });

    [Fact]
    public Task Without_an_answer_from_the_server_the_app_stands_alone() => HeadlessAvalonia.RunAsync(async () =>
    {
        var (dialog, shown, window) = await ShowAsync(null);
        var lines = LinesOf(window);

        Assert.Contains(Loc.AppName, lines);
        Assert.Contains(dialog.Client.BuildInfo, lines);
        Assert.DoesNotContain(lines, line => line.StartsWith("Home Automation Server", StringComparison.Ordinal));
        Assert.DoesNotContain(window.GetVisualDescendants().OfType<Separator>(), separator => separator.IsEffectivelyVisible);

        await dialog.AbortAsync();
        Assert.True(await shown);
    });

    [Fact]
    public void A_build_with_uncommitted_changes_does_not_claim_a_commit()
    {
        var info = new WebApiInfo { Configuration = "Debug", Branch = "dev", CommitId = "0123456789abcdef", IsDirty = true };

        Assert.Equal($"Debug:dev:{WebApiInfo.DeveloperBuild}", info.BuildInfo);
    }
}
