import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

const config = dotnetRuntime.getConfig();

// The app is handed its own base address - document.baseURI, which is the "/" of the base element in index.html,
// not location.href. The address of the current view is a view like /inverterdetails/Fronius/1234, and the
// api of the server does not live below that.
await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.document.baseURI]);
