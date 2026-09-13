---
paths:
  - HomeAutomationClient/HomeAutomationClient/ViewModels/MainViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/UserManagementViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/UserEditorViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/UserManagementView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/UserEditorView.axaml
  - HomeAutomationClient/HomeAutomationClient/Models/UserManagementEntry.cs
  - HomeAutomationServer/Controllers/IdentityController.cs
  - Fronius/Models/WebApi/UserAccount.cs
  - Fronius/Models/WebApi/UserInfo.cs
  - Fronius/Contracts/HomeAutomationClient/IWebClientService.cs
  - Fronius/Services/HomeAutomationClient/WebClientService.cs
  - HomeAutomationServer/Models/Authorization/User.cs
  - HomeAutomationServer/Models/Authorization/UserList.cs
  - HomeAutomationServer/Models/Settings/Settings.cs
  - HomeAutomationServer/Services/AuthenticationService.cs
  - HomeAutomationServer/Program.cs
  - Fronius/Localization/Resources.resx
  - Fronius/Localization/Resources.Designer.cs
---

# User management

Add/edit/delete users, reachable from the Settings menu next to the devices. `UserManagementEntry.Instance` is the
marker `MainViewModel.SettingsItems` appends; `MainViewModel.Settings(object)` recognises it and opens
`UserManagementViewModel` instead of a device's settings dialog.

## The client shows the entry to everyone, on purpose

`SettingsItems` puts `UserManagementEntry.Instance` in the list unconditionally, not only for an administrator.
This looks wrong and isn't: the client is not where user management is secured. Every endpoint in
`IdentityController` carries `[BasicAuthorize(Roles = nameof(Roles.Administrator))]`, and that is the only check
that has to hold. Showing the entry to a non-administrator just means they get a clean "403 Forbidden" message box
(`ViewModelBase.ShowHttpError`) instead of the menu item quietly not being there - which is exactly what makes it
possible to test the server-side enforcement from an ordinary account instead of having to demote an admin first.

Do not reintroduce a role check around the entry in `SettingsItems` to "clean up" the menu. If the product one day
wants the entry hidden from non-administrators again, that is a deliberate UX change to ask for, not a bug fix -
and the server-side `[BasicAuthorize]` must stay regardless.

## Renaming a user

The password hash (`User.GetHash` in `HomeAutomationServer/Models/Authorization/User.cs`) is `Salt` + password
bytes only - the username plays no part in it. So `PUT Identity/users/{userName}` treats the route segment as the
user's *current* name (the key used to find them) and `UserAccount.UserName` in the body as the name to rename them
to, which may be unchanged. `IdentityController.UpdateUser` rejects a new name already used by someone else.

On the client, `UserEditorViewModel.UserName` is editable in both add and edit mode (no `IsNew` gate). If an
administrator renames themselves, `UserManagementViewModel.Edit()` keeps the *original* name to key the PUT call,
then logs back in with whichever is the new identity (name and/or password) once the server confirms the change,
via `StoredConnection.SaveAsync` - the Basic Auth header the client was using stops working the instant the server
saves the change, so this has to happen before any further call.

## A user cannot delete themselves

`IdentityController.DeleteUser` rejects `userName` equal (ordinal, case-insensitive) to `HttpContext.User.Identity.Name`
with `422 Unprocessable Entity`. This has replaced `IsLastAdministrator` as the guard against deleting away the last
administrator: deletion is already restricted to administrators (`[BasicAuthorize(Roles = nameof(Roles.Administrator))]`),
so the only way the sole remaining administrator could ever be deleted is by themselves - a second administrator
would have to exist to delete the first one, and deleting *that* one still leaves the second. Blocking self-delete
therefore makes it impossible to reach zero administrators through deletion, without needing to count administrators
at all. `IsLastAdministrator` still exists and is still used in `UpdateUser` to stop the last administrator from
demoting themselves (a different code path with no self-delete check to fall back on) - do not remove it there, and
do not reintroduce it in `DeleteUser`. `UserManagementTests.An_administrator_cannot_delete_their_own_account` and
`A_different_administrator_can_delete_an_administrator_who_could_not_delete_themselves` cover this split. Renaming
yourself (above) is still allowed - only deletion is blocked.

## No repeat-password box

`Controls/PasswordBox.cs` already has a reveal icon (an eye button toggling `RevealPassword`) built in, so typing
the password once and looking at it is enough confirmation. `UserEditorViewModel` has no `RepeatPassword` property
or matching validation - do not add one back beside the `PasswordBox`.

## Server side: where users live and how passwords are hashed

There is no database. `Settings.Users` (a `HashSet<User>`, `HomeAutomationServer/Models/Settings/Settings.cs`) is
one field among the rest of the server's configuration, and is serialized as part of the same `Settings.xml` that
holds the Fritz!Box, Gen24 and Modbus connections. `Settings.Save`/`Load` run under a single process-wide `Lock` and
write the whole file with `XmlSerializer` - adding, editing or deleting a user always means "load the whole file,
change the in-memory graph, write the whole file back", not a per-user record update.

`User.PasswordHash` is `Convert.ToBase64String(SHA3_512.HashData(Salt bytes + UTF8 password bytes))`. `Salt` is 8
random bytes (`RandomNumberGenerator.GetBytes(8)`), generated once per user in the field initializer and replaced
every time `SetPassword` is called. The username plays no part in the hash (see "Renaming a user" above) - only
`Salt` does, so two users who happen to pick the same password still get different hashes.

`User.Authenticate` first checks an in-memory `passwordCache` (the plaintext password from the last successful
authentication on this `User` instance) before falling back to recomputing the SHA3-512 hash. This is a deliberate
performance trade-off, not an oversight: Basic Auth resends credentials on every request, and the server would
otherwise re-hash on every single API call from every logged-in client. Holding one cleartext password per `User`
object in server memory (never persisted, cleared whenever `PasswordHash` or `Salt` is set) is the accepted cost.
Do not flag this as a credential-storage bug.

`User.ClearTextPassword` is a write-only XML property (`get => null`) that exists purely as a one-way migration
path: if `Settings.xml` ever contains `ClearTextPassword="..."` (e.g. a hand-edited or legacy file), the setter
hashes it into `PasswordHash`/`Salt` on load, and because the getter always returns `null` combined with
`[DefaultValue(null)]`, `XmlSerializer` never writes a cleartext password back out on save. There is no code path
that persists a plaintext password.

### `UserList` is a live view of `Settings.Users`, not a copy

`UserList : AuthenticationSchemeOptions` exists only so ASP.NET's options system (`IOptionsMonitor<UserList>`) can
hand the same user collection to the authentication handler (`AuthenticationService`), `HubTicketService`/
`HubTicketAuthenticationService`, and `IdentityController`. `Program.cs` wires it up with
`.Configure<UserList>(u => { u.Users = settings.Users; })` - this assigns the *same* `HashSet<User>` reference that
`Settings` owns, it does not clone it. That is why `IdentityController.AddUser`/`UpdateUser`/`DeleteUser` mutate
`userDb.CurrentValue.Users` directly (`.Add`, `.Remove`, or property setters on a `User` found in it) and then call
`settings.SaveAsync()`: the mutation and the `Settings` instance being saved are the same object graph, there is no
separate step to "write the change into settings" first. If you ever change `UserList.Users` to be reassigned or
`IOptionsMonitor` reload were introduced, this shared-reference assumption would break silently.

### Login and the `auth` cookie carry the password in the clear (by design, over HTTPS)

`IdentityController.Login` and `AuthenticationService.HandleAuthenticateAsync` both work with Basic Auth
credentials (`user:password` base64-encoded), and `Login` stores that same Base64 string in a cookie so the browser
resends it automatically. The server never stores or logs the plaintext password - it only ever computes
`Authenticate(password)` against the hash - but the wire format is standard HTTP Basic Auth, which is why this
system is only appropriate behind TLS.

Basic Auth itself is a temporary stand-in, kept specifically because it lets the API be exercised straight from a
browser (Swagger/`requestKey`/manual URL testing) without building a login UI first. Do not read it as the intended
long-term scheme - if/when it is replaced (e.g. with a token/cookie-only scheme that doesn't need credentials on
every request), the cookie-carries-Basic-header approach in `Login`/`Logout` and the credential parsing in
`AuthenticationService` are the parts to revisit together.
