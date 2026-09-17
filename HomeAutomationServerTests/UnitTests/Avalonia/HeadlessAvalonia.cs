using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using De.Hochstaetter.Fronius;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.Services;
using De.Hochstaetter.HomeAutomationClient.ViewModels;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The application these tests run in: the Fluent theme, because the controls under test use its resources, and
/// the client's own loading indicators, because the busy animation of a dialog window is built from them.
/// </summary>
/// <remarks>
/// Not the client's <c>App</c>. That one builds the whole container and puts the main window up, which needs a
/// server to log in to; these tests want an application and nothing else.
/// </remarks>
public sealed class HeadlessTestApplication : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());

        Resources.MergedDictionaries.Add(new ResourceInclude(new Uri("avares://HomeAutomationClient/"))
        {
            Source = new Uri("avares://HomeAutomationClient/Assets/LoadingIndicators.Avalonia/LoadingIndicators.axaml"),
        });
    }
}

/// <summary>
/// One headless Avalonia session for the whole test run, and the one thread its windows live on.
/// </summary>
/// <remarks>
/// <para>
/// Everything Avalonia does has to happen on that thread, so a test body is handed to <see cref="RunAsync"/>
/// rather than run where xUnit started it. The session is started once and kept: starting one costs a UI thread
/// and a platform, and windows are global to it anyway.
/// </para>
/// <para>
/// <b>These tests must not run in parallel with each other.</b> Windows, the focus and the container are one per
/// session, so two tests at once would see each other's. <see cref="AvaloniaCollection"/> is what serializes them;
/// put every test that touches Avalonia in it.
/// </para>
/// </remarks>
public static class HeadlessAvalonia
{
    /// <summary>
    /// A classic desktop lifetime, which is what makes these tests test the desktop head: the presenter asks it
    /// for the active window to own a new one, and <see cref="Windows"/> is how the tests see what is open.
    /// <c>HeadlessUnitTestSession</c> is not used for that reason - it brings a lifetime of its own that has no
    /// windows at all, so every assertion here would be about something else.
    /// </summary>
    private static readonly ClassicDesktopStyleApplicationLifetime lifetime;

    /// <summary>
    /// Explicit, and not a field initializer: a class with only field initializers is <c>beforefieldinit</c>, so
    /// the runtime may put the initialization off until a static <b>field</b> is read - and <see cref="RunAsync"/>
    /// reads none. Avalonia was never started, the posted work sat in a dispatcher with no loop behind it, and the
    /// run hung with no output at all. A static constructor makes initialization happen before any member is used.
    /// </summary>
    static HeadlessAvalonia()
    {
        lifetime = StartUiThread();
    }

    /// <summary>
    /// Starts the UI thread and waits until Avalonia is up on it.
    /// </summary>
    /// <remarks>
    /// <b>Everything the new thread uses is a local of this method.</b> Reading a static of this class from there
    /// would block it on the class initializer that is running right here, while this thread waits for the new one
    /// to report that it has started - a deadlock that looks exactly like a test run that never produces output.
    /// </remarks>
    private static ClassicDesktopStyleApplicationLifetime StartUiThread()
    {
        var started = new TaskCompletionSource();
        var desktop = new ClassicDesktopStyleApplicationLifetime { ShutdownMode = ShutdownMode.OnExplicitShutdown };

        var uiThread = new Thread(() =>
        {
            try
            {
                AppBuilder.Configure<HeadlessTestApplication>()
                    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true })
                    .SetupWithLifetime(desktop);

                started.SetResult();
            }
            catch (Exception ex)
            {
                started.SetException(ex);
                return;
            }

            desktop.Start([]);
        })
        {
            IsBackground = true,
            Name = "Avalonia test UI",
        };

        if (OperatingSystem.IsWindows())
        {
            uiThread.SetApartmentState(ApartmentState.STA);
        }

        uiThread.Start();
        started.Task.GetAwaiter().GetResult();
        return desktop;
    }

    /// <summary>Runs a test body on the UI thread and waits for it, assertion failures included.</summary>
    public static Task RunAsync(Func<Task> body) => Dispatcher.UIThread.InvokeAsync(body);

    /// <summary>
    /// Lets the dispatcher run what the test posted and the windows settle. Layout, showing a window and closing
    /// one are all queued work, so an assertion straight after the call that caused them would read the state
    /// before it happened.
    /// </summary>
    public static async Task SettleAsync()
    {
        for (var round = 0; round < 8; round++)
        {
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background).GetTask().ConfigureAwait(true);
            await Task.Delay(1).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Puts a container behind <see cref="IoC"/> with the presenters and whatever else the test needs, and takes
    /// the windows of the previous test down.
    /// </summary>
    /// <remarks>
    /// On top of <see cref="TestInjector.CreateServices"/>, never from an empty collection: the injector is one
    /// for the process, and the hosted server tests run beside these. <c>IdentityController</c> reaches for the
    /// <c>IAesKeyProvider</c> through it in a static field, and a container without one, in place at the moment
    /// that field is first read, left every login of the run answering 500 - which is how the power flow page's
    /// tests, by taking a second longer, made fifteen user management tests fail on 2026-09-16.
    /// </remarks>
    public static void Reset(Action<IServiceCollection>? register = null)
    {
        foreach (var window in Windows.ToList())
        {
            window.Close();
        }

        var services = TestInjector.CreateServices();
        register?.Invoke(services);
        IoC.Update(services.BuildServiceProvider());
    }

    /// <summary>Every window that is open, which is what most of these tests assert on.</summary>
    public static IReadOnlyList<Window> Windows =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.Windows : [];

    /// <summary>
    /// A main view model built by hand rather than resolved: nothing on the paths under test reads the update
    /// service, and the fake is there so the field is not null where something does.
    /// </summary>
    public static MainViewModel CreateMainViewModel(IPagePresenter pagePresenter, IDialogPresenter dialogPresenter)
    {
        var webClient = new WebClientService();
        return new MainViewModel(webClient, new Gen24LocalizationService(webClient), new FakeUpdateService(), new FakeUriService(), pagePresenter, dialogPresenter);
    }
}

/// <summary>The collection every Avalonia test belongs to, so that none of them runs beside another.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AvaloniaCollection
{
    public const string Name = "Avalonia";
}
