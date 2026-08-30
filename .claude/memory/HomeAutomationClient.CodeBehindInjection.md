---
paths:
  - HomeAutomationClient/HomeAutomationClient/App.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/Controls/**
  - HomeAutomationClient/HomeAutomationClient/Views/**
---

# Dependency injection in HomeAutomationClient code behind

Applies to every `*.axaml.cs` of the `HomeAutomationClient` project.

## Rule

A code behind must have a **public parameterless constructor** and resolve what it needs with
`IoC.TryGetRegistered<T>()`. It must not take its view model or any other dependency as a constructor parameter.

```csharp
public partial class BatteryDetailsView : ContentPage
{
    public BatteryDetailsViewModel ViewModel => (BatteryDetailsViewModel)DataContext!;

    public BatteryDetailsView()
    {
        InitializeComponent();
        DataContext = IoC.TryGetRegistered<BatteryDetailsViewModel>();
    }
}
```

## Why constructor injection breaks the XAML preview

Avalonia's XAML **runtime** loader - which is what the previewer/designer uses, and what any other XAML file uses
when it instantiates the control as an element - creates a control through its public parameterless constructor.
A code behind whose only constructor takes parameters has none, so:

- the build emits `AVLN3001`: *XAML resource "avares://…" won't be reachable via runtime loader, as no public
  constructor was found*,
- the designer cannot render the view, and
- the control cannot be used from another XAML file.

Compiled XAML still works, which is why the app runs fine and the problem shows up only as a warning and a dead
preview.

`TryGetRegistered`, not `GetRegistered`: in the designer no container is running (`IoC.Injector` is `null`).
`TryGetRegistered` returns `null`, so `DataContext` simply stays empty and the preview renders; `GetRegistered`
throws and kills it. The consequence is that `ViewModel => (T)DataContext!` may be `null` at design time - never
dereference it before `Loaded`.

## Converting constructor injection written by a human developer

Whenever you touch a code behind that still injects through its constructor, convert it:

```csharp
// before
public SomeView(SomeViewModel viewModel)
{
    InitializeComponent();
    DataContext = viewModel;
}

// after
public SomeView()
{
    InitializeComponent();
    DataContext = IoC.TryGetRegistered<SomeViewModel>();
}
```

The registration in `App.axaml.cs` (`AddSingleton<SomeView>()`, `AddTransient<SomeView>()`) stays unchanged - the
container just calls the parameterless constructor, and the view resolves the very view model the container would
have injected. Callers such as `MainViewModel.ShowDetails`, which do `IoC.Get<SomeView>().ViewModel.X = …`, are
unaffected.

Do not "fix" this by keeping the injecting constructor and adding a parameterless one that delegates to it. That
compiles, but the delegating constructor is still a service locator, `GetRegistered` would throw in the designer,
and it leaves two ways to build the same view. See [[ViewModelsForInteractionLogic]] for what belongs in the code
behind at all.

## Why the static service locator is safe here

Resolving from a static accessor normally hides dependencies and bypasses scopes. In `HomeAutomationClient` the
second objection does not apply: the project registers only **singletons and transients** - there is no
`AddScoped` and no `CreateScope` anywhere in it - so a single root provider lives for the whole process and
`IoC.TryGetRegistered<T>()` returns exactly the instance constructor injection would have handed over.

This reasoning is specific to this project. Do not carry it into a project that uses scoped registrations, where a
static resolve would silently reach past the current scope.

## Related

- [[BatteryDetailsView.Lifecycle]], [[InverterDetailsView.Lifecycle]], [[SmartMeterDetailsView.Lifecycle]],
  [[WattPilotDetailsView.Lifecycle]] - the four views this rule was established on.
