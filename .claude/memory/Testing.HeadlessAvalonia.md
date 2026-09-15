---
paths:
  - HomeAutomationServerTests/UnitTests/Avalonia/**
  - HomeAutomationServerTests/HomeAutomationServerTests.csproj
  - HomeAutomationClient/HomeAutomationClient/HomeAutomationClient.csproj
---

# Testing the client's windows and controls (headless Avalonia)

Since 2026-09-15 the presenters, the dialog windows and the zoom are tested with **real windows on the headless
platform**, in `HomeAutomationServerTests/UnitTests/Avalonia`. They were a throwaway console probe before; the
developer asked for them as tests that run with every build.

## How a test is written

```csharp
[Collection(AvaloniaCollection.Name)]
public sealed class SomethingTests
{
    [Fact]
    public Task It_does_the_thing() => HeadlessAvalonia.RunAsync(async () =>
    {
        HeadlessAvalonia.Reset(services => services.AddSingleton<IDialogPresenter>(presenter));
        …
        await HeadlessAvalonia.SettleAsync();
        Assert.Equal(2, HeadlessAvalonia.Windows.Count);
    });
}
```

- **`RunAsync` is not optional.** Everything Avalonia does has to happen on its UI thread, and xUnit starts a test
  on a pool thread. The body goes to the dispatcher and the returned task carries assertion failures back.
- **`SettleAsync` after anything that changes the tree.** Layout, showing and closing are queued work, so an
  assertion straight after the call that caused them reads the state from before.
- **`Reset` at the start of a test.** The session is one for the whole run, so windows outlive the test that
  opened them; `Reset` closes them and puts a fresh container behind `IoC`. A test that forgets it sees the
  windows of whatever ran before - which is exactly how the smoke test failed once the suite grew.
- **Every such test belongs to `AvaloniaCollection`**, which is defined with `DisableParallelization`. Windows,
  the focus and the container are global to the session; two tests at once would see each other's.

## The two traps, both of which cost a round

- **`HeadlessUnitTestSession` is not used.** It brings a lifetime of its own that has no windows, so
  `IClassicDesktopStyleApplicationLifetime` is null, `WindowPresenter.ActiveWindow` finds nothing and there is no
  list of open windows to assert on - the tests would be about something other than the desktop head. The session
  here is built the way the real head builds one: `AppBuilder.Configure<HeadlessTestApplication>().UseHeadless(…)
  .SetupWithLifetime(new ClassicDesktopStyleApplicationLifetime())`, on a thread of its own that then runs
  `Start`.
- **The class needs an explicit static constructor.** With only field initializers a class is `beforefieldinit`,
  so the runtime may put initialization off until a static **field** is read - and `RunAsync` reads none. Avalonia
  was then never started, the posted work sat in a dispatcher with no loop behind it, and the run hung with no
  output at all. The same section must also never read a static of that class from the new thread while the
  initializer is running, or the two deadlock; everything the thread uses is a local.

## Internals

`HomeAutomationClient.csproj` has `<InternalsVisibleTo Include="HomeAutomationServerTests" />`, which CLAUDE.md
allows for a test project. It is needed because `IUpdateService` declares an **internal** event, so
`FakeUpdateService` could not be written without it - and a `DispatchProxy` over that interface fails for the same
reason, which is worth knowing before trying one.

## What the tests cover

| Class | What |
|---|---|
| `WindowPresenterTests` | The desktop head: a window per dialog and per device page, reuse and activation, the close box through `AbortAsync`, the modal message box, a logout |
| `MainViewPresenterTests` | Every other head: the dialog frame, nesting, the busy text handover, one page per view type, the menu bar gate |
| `ZoomBoxTests` | Ctrl with the wheel and the keys, the limits, the steps, the scope on a view inside the window, the focused text box |
| `ZoomPinchTests` | Pinch, with its events synthesized |
| `HeadlessSmokeTest` | That the session is up at all - look here first when the whole collection fails |

**Not covered, and not coverable here:** real touch input, so the pinch recognizer itself is untested; and how any
of it looks, which only running the app shows.
