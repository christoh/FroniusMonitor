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
- **`.claude/memory` is yours to maintain, without being asked.** Whenever you change something a document there
  covers - a file in its `paths:`, or a statement in it that your change makes wrong - update the document in the
  same piece of work as the code. Do not wait to be told, and do not leave a document saying something is missing
  once you have built it. A memory that has to be corrected by hand afterwards was worse than no memory at all,
  because it was believed in the meantime.
- **`.claude/rules` is mine.** Follow everything in it, and change it only when I ask you to. Where you think a
  rule is wrong or is getting in the way, say so and let me decide - do not edit it and do not work around it
  quietly.
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
* Always guard logging with `if (Logger.IsEnabled(LogLevel.<WhatEverLevel>))`. This is important to avoid unnecessary string formatting and performance overhead when the log level is not enabled.

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
- Do not commit without asking the human developer first.
- Force pushes are **never** allowed by AI, with one exception: when running in the cloud, AI may force push a branch that it created itself (`--force-with-lease`, never a bare `--force`). Branches created by a human developer stay off limits.
- History rewrites need confirmation by a human developer.
- Commit under your own authorship, never under the human developer's: `git commit --author="<name> <email>"`. The author e-mail is always `christoph@hochstaetter.de`, whatever `.git/config` says - in the cloud that config carries the AI's own address. Only the name changes.
  - The committer is never touched: it is whoever runs git. On the developer's machine that is the human developer, in the cloud it is the AI identity the environment configures.
  - The author name must name the AI you are **and** the model that made a change, including its version. For
  example `Claude Code (Opus 5)`, `GitHub Copilot (GPT 5.6 Terra)`. The tool name on its own is not enough - which model wrote the change is part of the record.
  - If a human developer also made changes, commit under his authorship. Split each commit by authorship. That includes splitting by AI model and splitting by human and AI. You know that in advance so make sure to keep a history of authorships that you can use later.
  - If you cannot find out which AI model made a change, ask me. Do not guess without asking.
