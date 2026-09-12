---
paths:
  - HomeAutomationClient/HomeAutomationClient/ViewModels/**
  - Fronius/Models/BindableBase.cs
---

# What ViewModelBase already gives you

Every view model of `HomeAutomationClient` derives from `ViewModelBase`, so check this list before writing
plumbing of your own - most of it is already there.

```
ViewModelBase          HomeAutomationClient/ViewModels/ViewModelBase.cs   (abstract, partial)
  BindableBase         Fronius/Models/BindableBase.cs                     (shared with server and WPF app)
    ObservableValidator                                                   (CommunityToolkit.Mvvm)
      ObservableObject
```

## ViewModelBase

- **`BusyText` / `IsBusy`** - the busy overlay (`c:BusyAnimation`) binds to them. `IsBusy` is `BusyText != null`,
  so `BusyText = string.Empty` means "busy, but show no caption" and `BusyText = null` means "not busy". The
  property is `virtual`, so a view model may override it to route the busy state somewhere else.
- **`Task Initialize()`** - virtual, does nothing by default. Views call it after setting the `DataContext`
  (`_ = viewModel.Initialize();`), so it is the place for async start-up work.
- **`TaskExceptionHandler(Func<Task> task)`** - wrap the body of a `[RelayCommand]` in it: it awaits the task,
  shows any exception in a message box and always clears `BusyText` afterwards. See the commands in
  `DashboardViewModel`.
- **`static ShowHttpError<T>(ApiResult<T> result)`** - the standard message box for a failed server call, with a
  special text for `403`. Being static, controls call it too (`InverterControl.axaml.cs`).
- **No `ConfigureAwait(false)` in a view model before it touches command state.** A property that a
  `[RelayCommand]`'s `CanExecute` depends on raises `CanExecuteChanged` straight into the bound buttons, and
  Avalonia throws `The calling thread cannot access this object` when that happens off the UI thread. A command
  body that awaits with `ConfigureAwait(false)` and then sets such a property (the `IsSending` of
  `ToshibaHvacViewModel` did) continues on a pool thread and crashes exactly there. Bindings marshal for you;
  `NotifyCanExecuteChanged` does not. Leave the awaits plain so the continuation returns to the UI thread the
  click came from.

## BindableBase

- **`IsNotifying`** - set it to `false` to suppress `PropertyChanged` while a batch of properties is written, then
  call **`Refresh(true)`** to raise one notification for everything and switch notifications back on. Use this
  instead of inventing a reentrancy flag; `InverterDetailsViewModel.ShowAll` and its reset command do exactly that.
  - **`Refresh()` without the argument would leave notifications off**: it restores the *previous* `IsNotifying`,
    which is `false` inside the batch. Always `Refresh(true)` when you turned notifications off yourself.
  - `Refresh` raises `PropertyChanged` with an empty property name, which the binding engine reads as "everything
    changed". It works regardless of `IsNotifying`. It does **not** force a value onto a control that has drifted
    away from it: Avalonia re-reads and compares, and pushes only what has actually changed. So `Refresh` is no way
    to put a text box right that holds something the model refused - see [[Validation.Lifecycle]].
- **`IsNotifyingBeforeChanging`** - off by default; turn it on to also get `PropertyChanging`.
- **`SetProperty(ref field, value, postAction, preFunc, propertyName, notifyAlways, comparer)`** - for properties
  that `[ObservableProperty]` cannot generate. `preFunc` coerces the incoming value and returns what to store,
  `postAction` runs after the change (for example to notify dependent properties), `notifyAlways` notifies even
  when the value did not change. Coerce with `preFunc`, but do not **validate** with it: a setter that refuses a
  value leaves the control holding it with nothing to replace it. Rules are attributes on the property; see
  [[Validation.Lifecycle]].
- **`Set(...)`** and **`NotifyOfPropertyChange(...)`** - Caliburn.Micro compatible aliases of `SetProperty` and
  `OnPropertyChanged`.

## From CommunityToolkit.Mvvm

- Source generators: `[ObservableProperty]` (on partial properties), `[RelayCommand]`, `[NotifyPropertyChangedFor]`,
  `[NotifyCanExecuteChangedFor]`. A calculated property stays a plain property and gets
  `[NotifyPropertyChangedFor(nameof(ThatProperty))]` on everything it depends on.
- From `ObservableValidator`: `INotifyDataErrorInfo`, `ValidateProperty`, `ValidateAllProperties`, `HasErrors`,
  `GetErrors` and the `DataAnnotations` attributes.

## Careful: three classes share the name

`FroniusMonitor` (WPF) and `FroniusPhone` (MAUI) have their own `ViewModelBase`, unrelated to this one except that
they also derive from `BindableBase`. The WPF one adds a `Dispatcher` and tracks WPF `ValidationError`s, the MAUI
one only adds an `IDispatcher`. Nothing written above about `ViewModelBase` applies to them; everything about
`BindableBase` does.
