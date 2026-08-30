---
paths:
  - FroniusMonitor/**
  - HomeAutomationClient/**
---

# View models own the interaction logic

This applies to all XAML UI projects: `FroniusMonitor` (WPF) and `HomeAutomationClient` with its head projects
(Avalonia).

## Rule

Any logic that decides *what the UI does* belongs in the view model, not in the code behind and not in XAML.
That includes:

- **UI state**: which parts are visible, expanded, selected, enabled or busy. Model it as bindable properties
  (`[ObservableProperty]`) and bind the controls to them.
- **Reactions to user input**: a click, a toggle, a selection change. Use `[RelayCommand]` and `Command=`, or a
  two-way bound property, instead of a `Click=` / `IsCheckedChanged=` handler in the code behind.
- **Rules between controls**: "this switch turns all the others on", "this is only enabled while that is filled
  in". Put the rule in the view model and let every control bind to the result.

A human developer may override this rule in a specific case, if the view model would become unreasonably complex otherwise. If you are told to use logic directly in the code behind, make sure you understand why and document it in a comment.

If you judge that code behind makes more sense than a view model implementation, inform the human developer why you think so, and offer to implement it. If the human developer disagrees, document the reason in a comment.

## Consequences

- **Do not bind one control to another to carry application state.** `ElementName` / `{Binding #Something}` is for
  purely visual relationships (a caption that mirrors a control's own size, for instance). State that another
  control also cares about goes through the view model.
- **Do not reach for `x:Name`d elements from the code behind** to read or write UI state. If the code behind needs
  to know a value, the view model should already own it.
- Views stay declarative, so the same view model survives a change of the view - which is exactly what the port
  from WPF to Avalonia keeps needing.

## What may stay in the code behind

Only work that genuinely needs the UI framework, and it should hand its result to the view model rather than act on
its own:

- Resolving theme resources and reacting to theme changes - see `DashboardView.UpdatePowerFlowColors`, which reads
  the theme brushes and passes plain colors into the view model.
- Attached properties, control templates, focus handling, animations.
- Marshalling to the UI thread (`Dispatcher.UIThread`), because the view model must stay free of UI framework types.

A view model must not reference Avalonia or WPF types. Colors go in as `De.Hochstaetter.Fronius.Models.HaColor`,
never as `IBrush` or `Color`.

## Example

The gauge group switches of `InverterDetailsView`: every switch is `IsChecked="{Binding <Group>, Mode=TwoWay}"`,
every group box is `IsVisible="{Binding <Group>}"`, and the `ShowAll` master switch is a view model property whose
setter writes all groups - no `x:Name`, no `ElementName`, no event handler in the code behind.
