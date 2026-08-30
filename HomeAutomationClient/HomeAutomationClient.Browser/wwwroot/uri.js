// The address bar. The app is a single page, so the address is changed with the History API: it never reloads
// anything, it only tells the browser (and the user, and a bookmark) which view is on screen.

// The path of the current address without scheme and host, always starting with a slash: "/" or
// "/inverterdetails/Fronius/12345678". Percent escapes are left exactly as they are.
export function getPath() {
    return globalThis.location.pathname;
}

// Shows path as the address and adds a history entry, so that the back button of the browser leads where the
// user came from.
export function pushPath(path) {
    globalThis.history.pushState(null, '', path);
}
