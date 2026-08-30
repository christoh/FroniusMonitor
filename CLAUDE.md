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
