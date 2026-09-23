---
paths:
  - HomeAutomationClient/HomeAutomationClient/ViewModels/MainViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/UserManagementViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/UserEditorViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/UserManagementView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/UserEditorView.axaml
  - HomeAutomationClient/HomeAutomationClient/Models/UserManagementEntry.cs
  - HomeAutomationClient/HomeAutomationClient/Views/MainView.axaml
  - HomeAutomationClient/HomeAutomationClient/Contracts/IUpdateService.cs
  - HomeAutomationClient/HomeAutomationClient/Services/UpdateService.cs
  - HomeAutomationServer/Controllers/IdentityController.cs
  - Fronius/Models/WebApi/UserAccount.cs
  - Fronius/Models/WebApi/UserInfo.cs
  - HomeAutomationClient/HomeAutomationClient/Contracts/IWebClientService.cs
  - HomeAutomationClient/HomeAutomationClient/Services/WebClientService.cs
  - HomeAutomationServer/Models/Authorization/User.cs
  - HomeAutomationServer/Models/Authorization/UserList.cs
  - HomeAutomationServer/Models/Authorization/AuthorizationExtensions.cs
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

## Change password and Log out live behind the user's own name, not in Settings

`MainView.axaml` turns the "who is logged in" text at the right of the menu bar into a `Button` (same
`MenuButtonStyle` as Dashboard/Details/Settings) whose `Flyout` offers `Loc.ChangePassword` and `Loc.LogOut`,
bound to `MainViewModel.ChangePasswordCommand` and `LogoutCommand`. Neither is in `SettingsItems` any more (the
`ChangePasswordEntry` marker class is gone, `SettingsItems` is only devices plus `UserManagementEntry`) - the
account you are logged in as is not a thing to configure, so it sits with your name rather than with the devices.

**A `StaticResource` on a sibling cannot see a `Resources` dictionary declared on another sibling** - only on
itself or an ancestor. `MenuButtonTemplate`/`MenuButtonStyle` used to live in `<StackPanel.Resources>` on the left
button row; the user button is a sibling of that `StackPanel` (both children of the row's `Grid`, overlapping
because the `Grid` has one cell and the right button is `HorizontalAlignment="Right"`), so once it also needed
`Theme="{StaticResource MenuButtonStyle}"` the lookup failed. Nothing errors when that happens - `StaticResource`
resolution is a runtime concern, not a compile-time one, unlike a compiled `{Binding}` - so the button silently
fell back to Fluent's default pill-shaped chrome instead of the flat menu look, and it was invisible except as a
grey capsule where the login row should have blended in. Caught by rendering headless with Skia - the developer's
endorsed method for any visual defect, see the session memory - and fixed by moving both `.Resources` and
`.Styles` from the `StackPanel` up to the row's own `Grid`, which both siblings can reach. If another sibling
needs `MenuButtonStyle` later, it already can; if the resources ever have to be scoped to only one side of the
bar, they need their own dictionary on that side, not a shared one moved back down.

## Logging out

`MainViewModel.Logout` confirms (`Buttons = [Loc.LogOut, Loc.Cancel]`, the same `Index != 0` pattern as
`UserManagementViewModel.Delete`), then hides the bar and whatever view was on screen exactly the way the app
looks before the first login (`IsReady = false`, `MainViewContent = null`), tears the session down, and shows the
login dialog again through `LoginAndStartAsync` - the private method `Initialize()` now calls too, so there is
one login flow, not two. Order matters: `IUpdateService.StopAsync` closes the hub connection **before** it clears
`Inverters`/`AllPowerConsumers`/etc., so nothing the hub might still deliver races the clearing and repopulates a
collection the login screen is about to hide anyway. `IWebClientService.Logout` clears the `Authorization` header
the `HttpClient` carries (regardless of whether the server could be reached) and calls `GET Identity/logout`,
which now answers `Ok(true)` - it used to answer `Ok()` with no body, harmless while nothing called it, but a typed
`ApiResult<bool>` needs something to deserialize. **Logging out forgets the stored credentials** (since
2026-09-23, at the developer's request): `StoredConnection.ForgetAsync` removes `CacheKeys.Connection` - user name
and encrypted password - through `ICache.RemoveAsync`, so the login box comes up empty and the next start does not
log the previous user in. The server address (`CacheKeys.ApiUri`/`HubUri`) is **kept**: it is not a credential, and
without it the next user would first have to know where the server is. The call has its own
`TaskExceptionHandler`, so a cache that cannot be written is reported but does not stop the logout before the login
box is up. Before that date the cache was deliberately left alone so the box came up pre-filled; that is gone.
`LoginAndStartAsync(followStartupPath: false, tryCachedLogin: false)` is what `Logout` passes: a startup deep link
has already been resolved once and there is nothing left to follow the second time, so it always lands on the
dashboard, and the cached credentials are **not** tried - there are none left, and where forgetting them failed,
the attempt would put the user who has just logged out straight back in.

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

**Guests are the one exception, and it is a different thing.** Since 2026-09-14 `MainView.axaml` binds the whole
Settings button to `MainViewModel.ShowSettingsMenu`, which is `User.Roles.SeesAllDevices()` - false for a login that
holds `Roles.Guest` and nothing more. That was asked for ("guests cannot change settings"), it hides the menu
for a guest rather than an entry for a non-administrator, and the server enforces it regardless: no guest holds
Operator or Administrator, so every settings endpoint refuses them anyway. What else a guest does and does not
see is in `SignalR.MessageDirection.md`, section "What a guest sees".

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

`SaveAsync` asks the server for the AES key belonging to the (possibly new) user name, and answers with a
`ProblemDetails` where that failed. It then writes **nothing**: a connection encrypted with the old key and read
back with the new one is a cached password that silently decrypts to empty. Every caller has to show that problem
with `ErrorBoxes.ShowServerProblem` and stop, rather than carry on believing the credentials are stored.

The username `TextBox` in `UserEditorView.axaml` must therefore carry **no `IsEnabled="{Binding IsNew}"`**. It did
once, and the result was a rename that failed in complete silence: in edit mode `IsNew` is false, so the box was
disabled, nothing the user typed reached `UserEditorViewModel.UserName`, and `Ok()` built a `UserAccount` holding
the name the user already had. The client then sent a PUT that asked the server to rename the user to their own
name, so every layer behaved perfectly and reported success - `200 OK`, a saved `Settings.xml` whose bytes happened
not to change, and a reloaded list still showing the old name. Roles and password kept working throughout, which
made it look like a server-side persistence bug rather than a disabled control. If a rename ever appears to do
nothing again, read the request body in the server log (`UpdateUser` logs the requested new name) before suspecting
`SaveAsync`.

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

### Every change must reach `Settings.xml`, and a test says so

Add, edit and delete each end in `await settings.SaveAsync()`, and that is the whole persistence mechanism - there
is nothing that flushes later, so an endpoint that forgets the call leaves a change that exists only until the next
restart. `UserManagementTests.Adding_editing_and_deleting_are_all_written_to_the_settings_file` and
`An_administrator_renaming_themselves_is_written_to_the_settings_file` pin this down by reading the file back with
the real `Settings.Load` instead of searching the text, so an entry that is written but no longer deserializes
fails too. A rename is the case worth testing on its own: the name is only the key the user is found by and is not
part of the password hash, so nothing else in the file changes with it, and a lost rename looks identical to a
rename that was never requested.

`UpdateUser` logs the requested *new* name and roles alongside the old name for the same reason - when a rename
appears to do nothing, the request body is the only thing that distinguishes "the server ignored it" from "the
client never sent it", and that has to be readable from the server log without a debugger attached.

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

### Somebody must be able to administer the server, or it does not start

`Program.EnsureAdministratorExists` runs at startup, after the settings are loaded and before anything is served,
and tells the two empty-handed states apart:

- **No users at all** is a fresh installation, and gets a user `admin` with the password `password` and
  `Roles.Administrator`, logged as a **warning that names both**. The point of the warning is that whoever reads
  the console can log in, so it has to be readable - and `admin`/`password` are hard-coded English, never
  localized, because they are typed into a login box whatever language the server runs in. The user is created
  with `SetPassword`, so only the hash reaches the file like any other.
- **Users, but none with `Roles.Administrator`**, is a mistake, and the server logs an **error and exits with
  code 2**. It cannot be repaired from a client: every endpoint that could grant the role is itself behind
  `[BasicAuthorize(Roles = nameof(Roles.Administrator))]`, so the only way out is a text editor and
  `Settings.xml`. Do **not** "helpfully" create the default administrator here as well - that would hand a login
  to anyone who can read the log, on a server that already has real accounts on it.

The created user is added to `settings.Users`, which is the very `HashSet<User>` that
`.Configure<UserList>(u => u.Users = settings.Users)` gave the authentication scheme, so it is live at once; the
`settings.SaveAsync()` at the end of `Main` is what makes it survive the restart. `StartupAdministratorTests`
covers both branches, including that the default administrator is never smuggled into a non-empty user list.
`HomeAutomationServer` has an `InternalsVisibleTo` for the test project so `Program` can stay internal.

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

### The built-in guest is in no user list, so nothing may search the list alone

Since 2026-09-16 `User.Guest` is a single static `User` named `guest`, with the password `guest` and
`Roles.Guest` and nothing else. It is not in `Settings.Users`, is never written to `Settings.xml`, and `GetUsers`
does not show it to an administrator - there is nothing to administer about it, because `AddUser`, `UpdateUser`,
`DeleteUser` and `ChangePassword` all refuse it with 422 and `Loc.GuestCannotBeChanged` /
`Loc.GuestCannotBeDeleted`.

**`Settings.EnableGuestAccount` switches it off**, and it is written to `Settings.xml` whichever way it is set -
no `[DefaultValue]` - so that a server's answer to "can anyone log in here" is in the file rather than implied by
an element nobody knew to look for. It defaults to `true`, which is what a file written before the setting existed
reads as, so an upgrade changes nothing. `Program.cs` copies it into `UserList.EnableGuestAccount` while it
configures the options: unlike `Users` that is a copy and not a live reference, so switching the account takes a
restart. `Program.LogGuestAccount` says at startup which way it went.

While the account is on the name `guest` is **reserved**: `Find` answers with the built-in one, and a user of that
name in `Settings.xml` is hidden and can never log in, which is what that startup warning is about. While it is
off the name means nothing in particular and an administrator may add, rename and delete a `guest` like any other
user. `AuthorizationExtensions.IsBuiltInGuest` is the one place that decides which of the two it is, and the
guards in `IdentityController` ask it rather than `User.IsGuest`.

`ChangePassword` is the guard that is easiest to forget and the one that matters most: it is the only one of the
four that needs no Administrator role, so the guest can reach it itself - and there is exactly one `User.Guest` in
the process, so a password change there would change it for every other guest until the next restart, saved
nowhere and repairable by nobody.

**Resolve a user name with `AuthorizationExtensions.Find(this UserList, string?)`, never with
`users.Users.FirstOrDefault(...)`.** Searching the list alone answers that the guest does not exist, and that is
not a theory: `IdentityController` knew about the guest while `HubTicketService.Validate` did not, so a guest was
issued a hub ticket that the hub then refused, and a guest who had logged in successfully saw nothing at all. The
callers are `IdentityController.FindUser`, `IdentityController.RequestKey`,
`AuthenticationService.HandleAuthenticateAsync` and `HubTicketService.Validate`. `Find` takes the first match, not
the single one, because two users of one name is a hand edited `Settings.xml` and failing every authenticated
request - including the ones needed to repair it - is the worse answer to that.

`User.CreateGuest` gives the guest a fixed salt instead of the random one `SetPassword` makes, so that everything
derived from it - the hub ticket signature, the key `RequestKey` hands the client - still means the same after a
restart. A salt has nothing to protect when the password is a constant in the same file.

`UnitTests/Hosted/GuestUserTests` covers the login, all four refusals and the hub ticket end to end;
`UnitTests/GuestAccountTests` covers the setting, its round trip through Settings.xml and both meanings of the
name; `UnitTests/HubTicketServiceTests` pins the lookup that broke.

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
