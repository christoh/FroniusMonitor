# Avalonia Desktop Version Enhancements

The avalonia desktop version can make good use of multiple Windows. We change the desktop version to support multiple windows. All other versions (mobile, browser should not be affected).

## Instructions
- The dashboard and the login dialog stay as they are (attached to MainView)
- Every other page (e.g. Details) becomes a separate non-modal Window.
- All dialogs also become Windows. They get the standard Windows chrome instead of the fake chrome.
- Dialogs that are currently resizable become resizable Windows. All others become non-resizeable Windows.
- Only message boxes and error boxes become modal Windows, others become non-modal Windows
- For everything that is non-modal:
  - if a window is already open, do not open a second window but activate the existing one
  - Some dialogs (e.g. settings) and pages (e.g. details) work with devices. The user should be able to open one Window for each device. This requires that some views and viewmodels that today register singleton must be registered transient.


