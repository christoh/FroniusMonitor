# General Rules
- If you create a memory, lifecycle contract or similar, create a virtual directory in "Solution Items" (only one for all files)
- Every memory, lifecycle contract or similar must start with a YAML header naming the repo-relative files it applies to:
  ```yaml
  ---
  paths:
    - HomeAutomationClient/HomeAutomationClient/Views/InverterDetailsView.axaml
    - HomeAutomationClient/HomeAutomationClient/ViewModels/InverterDetailsViewModel.cs
  ---
  ```
  Use full repo-relative paths (a trailing `/**` for a whole folder is fine), and list only the files whose editing
  actually requires the document. Read a document when your task touches one of its `paths:`, otherwise skip it, so
  that knowledge irrelevant to the current task is never loaded.
## Undo
* Make sure, that you can undo exactly your changes, especially if the git repo was dirty before you started editing. If you are unsure, please ask me before editing.

## Translation
* All logging and all test projects must be hard-coded and translated to English. If you find a localized string in logging,
remove it from the .resx file and replace it with an English string. Translate hard-coded non-English
text to English.

## C# dynamic type 
* dynamic is a problem in iOS.
* If you review, make a suggestion to refactor the code to use reflection (or any other appropriate coding that is no problem on iOS).
* If you edit a C# file and find dynamic, refactor the code to use reflection (or any other appropriate coding that is no problem on iOS).
* If you need to use dynamic, please add a comment explaining why it is necessary.

## Logging
* Guard logging with `if (Logger.IsEnabled(LogLevel.Debug))` or the appropriate log level. This is important to avoid unnecessary string formatting and performance overhead when the log level is not enabled.

## Error handling in the Avalonia client
* Avalonia has **no unhandled exception hook that works on every platform**. `AppDomain.CurrentDomain.UnhandledException`
  is a notification only - it cannot mark an exception handled and cannot keep the process alive - and it does not
  reach you in the browser head at all. `TaskScheduler.UnobservedTaskException` only turns up if and when the faulted
  task happens to be collected, which is far too late to tell the user anything. There is no portable equivalent of
  WPF's `Dispatcher.UnhandledException` with its `e.Handled = true`. The hooks that do exist are per platform and
  live in the head projects (Android and iOS each have their own), so they are no help in shared code.
* Consequence: **an exception that escapes takes the whole app down.** Everything the user can trigger has to be
  guarded at the point where it is started.
* `ViewModelBase.TaskExceptionHandler(Func<Task>)` is that guard for async work. It shows the exception and clears
  `BusyText`. Use it instead of a hand written `try`/`catch`/`finally`, so one place decides how a failure is
  reported. `MainViewModel.Initialize`, `ShowDetails` and `ShowDashboardView` are the pattern to copy.
* Never put `_ =` in front of a call that can throw. A fire and forget task that fails has nobody to report to, so
  the error is either lost silently or ends the app. If the caller cannot await - an event handler, a
  `Dispatcher.UIThread.Post` callback - the method being called is the thing that has to be guarded.
* `TaskExceptionHandler` only covers `Func<Task>`. When something of another shape needs guarding - a `void` or
  `Action<T>` event handler, a `Func<T>`, a `Func<Task<T>>` that has to return a value - **add the matching overload
  next to it in `ViewModelBase`** rather than writing the `try`/`catch` at the call site.

## Copy & paste
* Copy & paste is an anti pattern. Before you duplicate something, spend the effort to put the common part in one
  place: a base class, a method, a generic type, an extension method, a converter.
* The same applies to XAML. Repeated markup belongs in a `Style`, a `ControlTheme`, a `ControlTemplate`, a
  `DataTemplate` or a control of its own, never in a second copy.
* This also applies to code you did not write. If you come across duplicates while reviewing or editing, say so
  and offer to merge them, even when the duplication was there long before your change.
* The exception is code that only looks alike and is expected to evolve apart. Merging that couples two things
  that have nothing to do with each other. Say why when you leave such a duplicate in place.

# Building
* Every project sets `<Configuration>Release</Configuration>`, so a plain `dotnet build` or `dotnet run` builds
  Release, which is noticeably slower. Add `-c Debug` whenever the build output itself does not have to be a
  Release one: compile and XAML checks, running the app locally, debugging.
* Stay with the default Release configuration when the result matters as such: publishing, measuring performance,
  or checking behavior that differs between the configurations (the Avalonia diagnostics package, for instance, is
  only in the Debug build).

# Unit tests
* In unit test entry points do not use `.ConfigureAwait(false)`. This could violate the test framework rules for not executing certain tests in parallel. `.ConfigureAwait(false)` is allowed and encouraged elsewhere in unit tests regardless, whether a method is public, private or internal. Besided from unit tests, `.ConfigureAwait(false)` is always allowed and encouraged where appropriate.
* .First(), FirstAsync(), etc. in IEnumerable and IQueryable as a replacement for .Single(), SingleAsync() etc. can speed up things and you are encouraged do to so if appropriate. In unit tests, we always use "Single" when we mean it because it can detect problems.
* There are unit tests projects using NUnit. These are legacy. We use xUnit for new unit tests. If you find a unit test project using NUnit, please create a new xUnit project and port the tests to xUnit. If you are unsure how to do this, please ask me before editing. Setup logging in any new unit test project. So that the logging abstractions used in the code, log to the test output.
* When performing unit tests, only do it for tests in the UnitTests subdirectory. All other tests require a specific communication environment setup and are likely to fail. This is normal.

# Commits
- Always suggest commit and push. Never commit alone.
- Force pushes are **never** allowed by AI.
- Commit under your own authorship, never under the human developer's: `git commit --author="<name> <email>"`. The e-mail always stays as in .git/config. Only the name changes.
  - The author name must name the AI you are **and** the model you are running on, including its version. For
  example `Claude Code (Opus 5)`, `GitHub Copilot (GPT Terra)`. The tool name on its own is not enough - which model wrote the change is part of the record.
  - If the working tree also holds changes made by the human developer, split the commit: commit your own changes under your AI authorship and leave theirs to them. Never sign a human's work with your name, or your own work  with theirs.
  - Committer should never be touched.

