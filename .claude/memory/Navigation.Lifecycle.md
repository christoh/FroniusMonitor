---
paths:
  - HomeAutomationClient/HomeAutomationClient/Contracts/IUriService.cs
  - HomeAutomationClient/HomeAutomationClient/Services/FakeUriService.cs
  - HomeAutomationClient/HomeAutomationClient/Misc/ViewPath.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/MainViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/Views/PowerFlowView.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/App.axaml.cs
  - HomeAutomationClient/HomeAutomationClient.Browser/Platform/UriService.cs
  - HomeAutomationClient/HomeAutomationClient.Browser/Program.cs
  - HomeAutomationClient/HomeAutomationClient.Browser/wwwroot/uri.js
  - HomeAutomationClient/HomeAutomationClient.Browser/wwwroot/main.js
  - HomeAutomationClient/HomeAutomationClient.Browser/wwwroot/index.html
---

# Lifecycle contract: navigation and the address of a view

Every view the user can reach has an address. In the browser that address is the address bar, so a view can be
bookmarked, sent to somebody and reached with back and forward; on the other heads it is a list nothing shows
yet. The app is a single page either way - **no navigation ever reloads anything.**

## The parts

| Part | Where | What it does |
|---|---|---|
| `IUriService` | `Contracts` | The address of the current view: `StartupPath`, `SetPath`, `PathChanged` |
| `UriService` | Browser head, `Platform` | The address bar, through `wwwroot/uri.js` and the History API |
| `FakeUriService` | `Services` | Desktop, Android, iOS: the same addresses collected in a list |
| `ViewPath` | `Misc` | Translates between a device and its address, in both directions |
| `MainViewModel` | `ViewModels` | The only caller: it shows a view and hands the address on |

`App.axaml.cs` registers `FakeUriService` with `TryAddSingleton`, so a head that has something better registers
it first and wins - which is exactly what the browser head does in `Program.Main`, with
`AddSingleton<IUriService>(await UriService.CreateAsync())` **before** Avalonia starts.

## The shape of an address

`/<view>/<manufacturer>/<serial number>` for a device, `/<page>` for a page that belongs to none, or `/` for the
dashboard.

```
/inverterdetails/Fronius/12345678
/batterydetails/BYD%20Battery-Box/P03T%2012%2F34
/powerflow
```

Manufacturer and serial number, because they are printed on the device and on its type plate: a user can read an
address, and can type one. They identify one device of an installation.

- **Escaping is `Uri.EscapeDataString`,** what a browser expects. A space becomes `%20` and stays a space. **Do
  not substitute characters** - an underscore for a space makes an address that cannot be turned back into the
  name it came from.
- **Do not use `IHaveUniqueId.Id` for an address.** It replaces spaces and slashes, so it does not survive the
  round trip. It has a different job.
- `ViewPath.Parse` compares **unescaped** values, so `%C3%A4` and `%c3%a4` are the same address, and matching is
  case-insensitive: a hand written link works.
- Add a device type in **one** place: the `switch` in `ViewPath.Identify` gives it a view, a manufacturer and a
  serial number, and both directions work at once.
- An address whose device this installation does not have is not an error: `ViewPath.Find` returns null and the
  user gets the dashboard.

**A page without a device is a constant, not a family.** The power flow page shows the installation as a whole, so
there is nothing to name in its address and exactly one of it: `ViewPath.PowerFlow` is the address, and
`ViewPath.IsPowerFlow` recognises it. It needs no `For` and no `Find`, and both it and `Parse` count segments
through the same `ViewPath.Segments`, so a leading or trailing slash means the same thing to all of them.

## Who writes the address, and when

`MainViewModel` is the only place that calls `SetPath`, from `ShowDetails`, `ShowPowerFlowView` and
`ShowDashboardView`. All three take an `updatesAddress` flag:

| Trigger | `updatesAddress` | Why |
|---|---|---|
| The user picks a device from the menu (`ShowDetailsCommand`) | `true` | This is a new place, it belongs in the history |
| The user picks the power flow page from the View menu (`ShowPowerFlowCommand`) | `true` | Likewise |
| Back or forward (`OnPathChanged`) | `false` | The address is already the one being navigated to |

**`MainViewModel.ShowPath` is the one place that turns an address into a view** - power flow page, detail view of
a device, or the dashboard where it is neither. Startup and the back button both go through it, so they cannot
come to disagree about what an address means; they differ in the `updatesAddress` flag alone. A new addressable
view is added there and nowhere else.

**A head with a window per detail page has one address and several pages.** The desktop opens a window for each
device (see [[DialogSystem.Lifecycle]]) and the address is still written on every one of them, so it names the page
most recently opened rather than "the" page. Nothing shows it there - `FakeUriService` only collects - so this
costs nothing today; a head that ever displays the address will have to decide what it means with five windows up.

**Following the browser must not write the address back.** Without the flag, a hand written link with lower case
escapes (`/x/%c3%a4`) would be resolved, then pushed again in its canonical spelling (`/x/%C3%84`) - a second
history entry in front of the one the user pressed back to, which they can never get past. `SetPath` also
compares before pushing and stays quiet when the address is the one already shown, but that check alone does not
cover the spelling case.

## Startup: a link into a detail view survives the login

1. The head builds the `IUriService`; the browser one reads `location.pathname` **once**, into `StartupPath`.
2. `MainViewModel.Initialize` logs in - with the cached credentials where they are complete, through the login
   dialog where they are not - and starts `UpdateService`.
3. Only **after** `IsReady` does it hand `StartupPath` to `ShowPath` - the devices of the installation are known
   only now - and show that view, or the dashboard.

`OnPathChanged` does nothing while `!IsReady` for the same reason: pressing back during the login has nothing to
resolve against yet, and step 3 does it afterwards anyway.

After a `Logout` the same code runs again with the dashboard in place of `StartupPath`: the address was reset when
the session was torn down, and a startup link must not be followed a second time.

## Back and forward

`uri.js` listens for `popstate` and hands the new path to .NET:

```js
export function onPathChanged(handler) {
    globalThis.addEventListener('popstate', () => handler(globalThis.location.pathname));
}
```

The browser `UriService` imports it as `[JSMarshalAs<JSType.Function<JSType.String>>] Action<string>` and raises
`PathChanged`; `MainViewModel` resolves the path on the UI thread and shows the view. **No login and no reload
are involved** - the session is untouched, the address has already changed, and nothing is written back.

- **The delegate is held in a field** (`pathChangedHandler`). It is the only reference the .NET side keeps to the
  callback the browser holds; do not pass a lambda straight into `OnPathChanged`.
- **Another origin needs no code.** Going back out of the app is a real navigation: the document unloads,
  `popstate` never fires, and the browser goes where it is told.
- `FakeUriService.PathChanged` has empty `add`/`remove` accessors: these heads have no back button of a browser,
  so a subscription is dropped on purpose rather than kept for an event that will not come. Give it a body when
  they get one - Android's system back button is the obvious candidate.

## What the browser head needs besides the History API

Two things, both about a **deep link**, and both easy to break again:

- **`<base href="/" />` in `index.html`.** Every asset is loaded relative to the root. Without it, a link to
  `/inverterdetails/Fronius/1234` makes the browser look for `main.js` and `_framework` next to that path, and
  the app never starts (404 on `main.js`).
- **`main.js` hands the app `document.baseURI`, not `location.href`.** `Program.Main` derives `CacheKeys.ApiUri`
  and `CacheKeys.HubUri` from `args[0]`, so with `location.href` the api and the hub of the server were looked
  for below the path of the *view* - the app started at a deep link, then failed to talk to the server.
  `document.baseURI` is the `/` of the base element, which is the root the server answers at - and that, not a
  path inside the app, is what `ServerUris.From` (the one place that puts those two addresses together) has to be
  given.

**The web server must serve `index.html` for an unknown path** (SPA fallback), otherwise a deep link is a 404
before any of this runs. Verified on the live site: `https://home.hochstaetter.de/inverterdetails/...` answers
200 with the app.

## The heads without an address bar

`FakeUriService` keeps `ObservableCollection<string> Uris`: every address the app has shown, oldest first,
without scheme and host, the current one last. Consecutive duplicates are not added. Nothing displays it today -
it exists so that whoever needs it next (a back button, restoring the last view on start, a shareable link)
finds a history that is already correct. `StartupPath` is the dashboard, because these heads have no address to
start from.

## Verified, so you do not have to measure again

- The `popstate` callback reaches .NET. Measured in Chrome against the dev server with a temporary
  `Console.WriteLine` in the handler: two `pushPath` calls, then back, back, forward gave exactly three
  callbacks, with the right path each time.
- The escaping survives the round trip: `/batterydetails/BYD%20Battery-Box/P03T%2012%2F34` came back unchanged.
- Changing the view updates the address bar, and a deep link reaches its view after the login (user confirmed on
  the live site).
- The live site serves `index.html` for an unknown path, so a deep link is the app and not a 404.
- `dotnet run` on the browser head renders nothing (see the platform heads document), but the .NET side runs and
  the browser console is enough to check this interop.

## Known gaps

- **Nothing but the four detail views and the power flow page has an address.** The settings dialog, the login and
  every other dialog are not addressable, and a dialog does not appear in the history. The price chart is a dialog
  rather than a page, so it has none either, although it sits beside the power flow page in the View menu.
- **A dialog is not part of the history.** Opening the settings or the login does not change the address, and
  back does not close one.
- The non-browser heads collect addresses nobody reads yet.
