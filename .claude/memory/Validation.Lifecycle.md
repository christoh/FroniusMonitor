---
paths:
  - Fronius/Validators/**
  - Fronius/Models/BindableBase.cs
  - Fronius/Models/Gen24/Settings/**
  - HomeAutomationClient/HomeAutomationClient/Styles/Validation.axaml
  - HomeAutomationClient/HomeAutomationClient/Styles/TextBoxes.axaml
  - HomeAutomationClient/HomeAutomationClient/Converters/ValidationConverters.cs
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/**
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/**
  - FroniusMonitor/Wpf/Validation/ValidationRules.cs
  - FroniusMonitor/Wpf/Converters/ValidationBinding.cs
---

# Lifecycle contract: validating what the user types

One mechanism for the whole solution: a rule is declared **on the property that is edited**, and every consumer
picks it up from there - the Avalonia client, the WPF app, and ASP.NET Core model binding on the server. Nothing
about validation belongs to a dialog. What the settings dialogs add on top is in [[SettingsDialogs.Lifecycle]].

## It only works if a text box binds to a string

`.claude/rules/ViewModelsForInteractionLogic.md` says a text input element never binds to a numeric property, and
everything here rests on that. A binding to a number cannot report what the user typed: as soon as the text does
not convert - a letter, an empty box, a value too large for the type - the binding refuses the write itself, the
property never changes, and **there is no property changed event**. From there the rule never sees the value, the
view model cannot tell the field is wrong, and Undo has nothing to push, because a binding only pushes what has
changed.

So a view model keeps a `string` of its own per numeric field, filled from the value when the dialog loads and
whenever Undo runs, written back on Apply. `Gen24ModbusViewModel.MeterAddressText` and `SunSpecAddressText` are
the worked example; `NumericText` in `Fronius/Validators` does the two conversions.

**`NumericText` reads a number exactly the way `MinMaxIntAttribute` and `MinMaxDoubleAttribute` do**, on purpose.
A view model that parsed differently from the rule that let the text through would throw at the moment of saving,
and only for some inputs. Its `ToInteger` / `ToNumber` therefore throw an `InvalidOperationException` rather than
guess: getting there means a rule and a caller disagree, which is a defect and not a user error.

## The rule is an attribute, the model reports, the view shows

```csharp
[ObservableProperty]
[NotifyDataErrorInfo]
[MinMaxInt(Gen24ModbusSettings.MinMeterAddress, Gen24ModbusSettings.MaxMeterAddress,
    MessageResourceKey = nameof(Loc.MeterAddressError))]
public partial string? MeterAddressText { get; set; }
```

The WPF app has no source generator for the toolkit - `CommunityToolkit.Mvvm` is not one of its package references, so
the analyzers never run there. Its properties are written by hand and call `ValidateProperty` themselves:

```csharp
[MinMaxInt(...)]
public string? MeterAddressText
{
    get;
    set => Set(ref field, value, postAction: () => ValidateProperty(value, nameof(MeterAddressText)));
}
```

- **`Fronius/Validators`** holds the rules: `MinMaxIntAttribute`, `MinMaxDoubleAttribute`, `RegexRuleAttribute`,
  `Ipv4Attribute`. Each one is a `Complaint` and an `IsAcceptable`; `ValidationRuleAttribute` does the rest -
  empties, and the message. The message is either composed around `PropertyDisplayName` /
  `PropertyDisplayNameResourceKey`, or named outright by `MessageResourceKey` where the field already has a
  sentence of its own. It is put together **while the rule runs**, never in the constructor, so it follows the
  current language.
- **A blank value passes unless `AllowEmpty = false`.** Not having typed anything is not the same as having typed
  something wrong. Where a value really is required, `AllowEmpty = false` refuses a blank with the field's own
  message - "must be between 1 and 247" says everything there is to say about an empty box, and about "abc" as
  well.
  **But a required field has to be validated after every load, not only when a setter runs.** `SetProperty` and
  the generated setters validate what they *store*, and storing the same value twice - null over null, at load or
  after Undo - stores nothing, so nothing is validated. A field whose loaded value is null therefore shows no
  error until the user touches it, and Undo back to null leaves the error from the last thing they typed standing.
  That is why the reset of a tab ends with `ValidateAllProperties()`: the validation state then always describes
  what is on screen rather than what happened to change. Neither Modbus address is required in the end - an empty
  one becomes null, and `GetUpdateToken` leaves a null out of the delta, so it means "not mine to say" and can
  never overwrite what the inverter holds.
- **Every rule reads the value from its text**, because that is what a text box gives it. So the rule, and not a
  converter, is what decides whether what was typed is a number at all.
- **`[NotifyDataErrorInfo]` is what makes it work.** `BindableBase` is an `ObservableValidator`, so the generated
  setter validates what it is given and reports through `INotifyDataErrorInfo`. Avalonia's `IndeiValidationPlugin`
  turns that into `DataValidationErrors` on the control, WPF picks it up through `ValidatesOnNotifyDataErrors`
  (on by default), and `[ApiController]` turns a refused request body into a `ProblemDetails` with the same
  wording. Without the attribute the rule is inert.
- **A view model validates as well as a model.** `ViewModelBase` is a `BindableBase` too, which is what lets the
  string a box binds to carry the rule. Where a limit then appears twice - on the string in the view model and on
  the property of the model, which is what the server checks a request body against - name it once and have both
  attributes use it: `Gen24ModbusSettings.MinMeterAddress` and the three beside it.

## A rule as an instance, where the limits are not known at compile time

`NumberField` (`HomeAutomationClient/ViewModels/Dialogs`) is one box-and-slider number of the Wattpilot dialog,
and there are thirty of them with thirty different ranges. An attribute cannot be parameterized per instance, so
the field takes **an instance of the rule** in its constructor - `new MinMaxIntAttribute(6, 32) { PropertyDisplayNameResourceKey = ... }`
- and asks it through `GetValidationResult` itself, reporting through `INotifyDataErrorInfo` of its own rather
than through `ObservableValidator`, whose validation only knows attributes on properties. It is the same rule with
the same words, instantiated at runtime exactly the way the WPF wrappers instantiate theirs. Avalonia's
`IndeiValidationPlugin` does not care which object reports, so the red frame and the tooltip work unchanged.
Everything else here still holds for it: the text is stored whatever it is, the slider is only moved by a value
the rule has passed, and a load validates explicitly because a setter only validates what it stores.

## A refused value is stored, on purpose

**A setter must not refuse to store what it was given.** One that throws the value away leaves the control holding
the refused text with nothing arriving to replace it, and worse: the next time the binding re-evaluates the error
clears while the text is still on screen, so an invalid value ends up *looking* valid, and typing the same value
again is a no-op so it does not even mark itself a second time. Store it, report it, and Undo can put it right,
because the value it restores genuinely differs from the one the binding last read.

## Undo

Replace the settings object and re-fill every string a box binds to, from the settings the dialog started with -
`Gen24ModbusViewModel.Reset`. That is unconditional: whatever the user typed went into a string property, so
restoring it is a real change and reaches the control. A fresh settings object also brings a fresh validation
state, so the red frames go with it.

The measured fact behind "unconditional" needing to be said at all: **an Avalonia binding pushes a value to its
control only when the value it reads has changed.** Raising `PropertyChanged` for every property does not force it
either. With numbers bound directly there was no way to make Undo whole; with strings there is nothing to force.

## What it looks like

`Styles/Validation.axaml` does it once for the whole app, and `App.axaml` includes it **after**
`Styles/TextBoxes.axaml`:

- a `ControlTheme` for `DataValidationErrors` that renders the field and nothing else. The default writes the
  message beside the field and reflows the group on every keystroke. Setting `DataValidationErrors.ErrorTemplate`
  is not the lever: the template that draws the message belongs to the control, not to the attached property.
- `ToolTip.Tip` on every `TextBox`, bound to `$self.(DataValidationErrors.Errors)` through the `ValidationErrors`
  converter, so the reason shows when the pointer rests on a refused field. `(DataValidationErrors.Errors)[0]`
  does **not** compile - a compiled binding cannot index an `IEnumerable<T>` - and the whole list is better anyway,
  because a field can be refused for more than one reason. The converter returns null for an empty list and
  Avalonia shows no tooltip for a null `Tip`, so a field that is fine has none. A box that sets a `Tip` of its own
  keeps it, because a local value beats a style setter.
- the red frame, `TextBox:error`, **after** the focus rules of `TextBoxes.axaml`. A value is refused while it is
  being typed, so the field is focused and in error at once, and of the two it is the error that has to show;
  styles of equal specificity apply in order. `ValidationErrorBrush` and `TextBoxFocusedBorder` are in both theme
  dictionaries - the accent colour of this app is a red, so a focused field painted with it read as a refused one.

`UpdateSourceTrigger=PropertyChanged` still has to be on every validated binding. Avalonia writes `TextBox.Text`
back on `LostFocus` by default, so without it nothing is validated while the user types. That is what
`ValidationBinding` of the WPF app does for its own bindings.

An `Exception` in `DataValidationErrors.Errors` rather than a `ValidationResult` means a binding refused a value
itself - which is what binding a text box to a string is there to prevent. The converter shows its message as it
is, because it is a defect to fix and not something a user can act on.

## Two routes through a converter that do not work

Both were tried with Avalonia 12.1.1 while looking for a way to validate at the binding, and both are traps:

- A converter whose `ConvertBack` **throws** does mark the field, but the message is Avalonia's
  `Could not convert '(unset)' (Avalonia.UnsetValueType) to System.String.` - there is nothing to show a user.
- A converter whose `ConvertBack` returns a `BindingNotification` with `BindingErrorType.DataValidationError`
  **writes the notification object itself into the model** as the value. No error is reported at all.

## Never MemberwiseClone an ObservableValidator

`MemberwiseClone` copies the *reference* to the error store of `ObservableValidator` while giving the copy an error
count of its own, so the two objects report validation states that contradict each other - `GetErrors` sees the
shared store, `HasErrors` only the count - and a copy made for Undo arrives carrying the errors of the object it
replaces. It also copies the `PropertyChanged` subscribers. `Gen24ModbusSettings.Clone` therefore assigns through
the properties, one by one; `Gen24ModbusSettingsTests` checks the copy against `GetToken`, which walks every field
that goes to the inverter and so catches one the hand written copy forgot. **Any other settings type that gains a
rule has to give up `MemberwiseClone` in the same way.**

## The WPF app gives its rules to a binding, and they are wrappers

`FroniusMonitor/Wpf/Validation/ValidationRules.cs` makes every rule available to XAML at the binding site, the way
WPF wants it: `Text="{co:ValidationBinding Path, Rule={v:MinMaxIntRule Minimum=0, Maximum=100}}"`. **None of those
extensions decides anything.** Each is three lines that build the matching attribute from `Fronius/Validators` and
hand the value to it, so both apps and the server refuse the same values with the same words. Adding a rule means
adding an attribute, and then a wrapper here only if WPF needs it at a binding.

- `ValidationRuleExtension` carries `MessageResourceKey`, `PropertyDisplayName`,
  `PropertyDisplayNameResourceKey` and `AllowEmpty`, and copies them onto the attribute it builds. It builds a new
  one per call, because a markup extension is given its properties after it has been constructed and XAML may hand
  the same extension to more than one binding.
- **`AllowEmpty` defaults to `false` there**, where the attributes default to `true`. Every field in the WPF app
  that carries a rule is one that has to be filled in, and that is what those rules did before they became
  wrappers. Set it in the markup where a field may be left empty.
- The message goes back through `StringResult`, which asks the rule again every time it is read, so one already on
  screen when the language changes answers in the new language.
- `ChargingRuleDate` used to say "Must bind to String" for a value that was not one; it now says the same as for
  any other unreadable time. That message was for a developer, and it is the only behaviour the move changed.

## Still open

- **`NotEmptyAttribute`** in `Fronius/Validators` is an empty `ValidationAttribute`, so it passes everything it is
  given. Nothing uses it - `AllowEmpty = false` is what says a field is needed. It needs an `IsValid` and a
  message of its own, or it needs to go.
- Only `TextBox` gets the tooltip. Any other control with `EnableDataValidation` is one selector away.
