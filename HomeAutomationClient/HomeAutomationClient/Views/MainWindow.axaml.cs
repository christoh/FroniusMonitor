namespace De.Hochstaetter.HomeAutomationClient.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Content = IoC.GetRegistered<MainView>();
        RestoreSize();
        Closing += (_, _) => SaveSize();
    }

    /// <summary>
    /// Opens the window as big as it was closed - see <see cref="StoredWindowSize"/>, which decides what is
    /// usable; here only the window's own properties and the screen it has to fit on are touched, which is what
    /// the code behind is for. No position is restored, so the OS places the window.
    /// </summary>
    private void RestoreSize()
    {
        // The working area comes in physical pixels, Width and Height are device independent.
        var screen = Screens.Primary ?? Screens.All.FirstOrDefault();
        var (maximumWidth, maximumHeight) = screen is { } ? (screen.WorkingArea.Width / screen.Scaling, screen.WorkingArea.Height / screen.Scaling) : (0, 0);

        if (StoredWindowSize.Load(IoC.TryGetRegistered<ICache>(), maximumWidth, maximumHeight) is not { } size)
        {
            return;
        }

        Width = size.Width;
        Height = size.Height;

        if (size.IsMaximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void SaveSize()
    {
        // A minimized window has no size worth keeping; what is stored stays as it is.
        if (WindowState == WindowState.Minimized)
        {
            return;
        }

        StoredWindowSize.Save(IoC.TryGetRegistered<ICache>(), ClientSize.Width, ClientSize.Height, WindowState is WindowState.Maximized or WindowState.FullScreen);
    }
}
