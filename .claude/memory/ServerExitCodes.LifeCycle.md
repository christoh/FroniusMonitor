---
paths:
  - HomeAutomationServer/Program.cs
  - HomeAutomationServer/Models/Settings/Settings.cs
  - HomeAutomationServer/Models/Authorization/User.cs
  - HomeAutomationServerTests/UnitTests/StartupAdministratorTests.cs
---

# Server exit codes

`HomeAutomationServer/Program.cs` owns the process-level exit codes. The startup path decides what to say, what to
write, and whether the process may continue at all.

## Startup success

The process exits `0` when `Main` reaches `app.RunAsync()` and the host stays alive until shutdown.

## Settings load failures

- **`FileNotFoundException`**: the file was missing, so `Program` creates a default `Settings.xml` and logs a warning.
  `Settings.SaveAsync()` is called immediately so the file exists for the next start.
- **Any other exception while loading settings**: the server logs a critical error and returns the exception's
  `HResult` as the process exit code. The server does not start.
- **`settings == null` after the load path**: the process exits with code `1`.

These values are intentionally not localized: they are not user-facing strings but process state and diagnostics.

## Default administrator creation

When `Settings.Users` is empty, `Program.EnsureAdministratorExists` creates a user named `admin` with the password
`password` and the `Administrator` role. It logs a warning naming both strings, because the console output is the
only way to know what the fresh installation can log in with without a settings editor.

The created user is saved through the same `Settings` graph the server keeps in memory, so it is part of the
`Settings.xml` written by `Settings.SaveAsync()` before the host starts serving requests.

## Missing administrator in a non-empty list

`Program.EnsureAdministratorExists` also rejects a non-empty list with no `Administrator` role. This is a server
misconfiguration, not a fresh-install case, so it logs an error and exits with **code 2**.

This is intentional: every user-management endpoint is guarded by `[BasicAuthorize(Roles = nameof(Roles.Administrator))]`,
so no client can repair a server state in which nobody has that role. The fix is a human editing the settings file or
removing the user list to trigger the default-admin creation path on the next start.

## Summary

- `0`: normal server run
- `1`: startup could not obtain a valid `Settings` instance
- `2`: the settings file has users but no administrator
- `HResult`: a fatal settings-load exception; process exits before the host starts
