namespace FroniusPhone.Pages
{
    public partial class AppShell : Shell
    {
        public AppShell(MainPage mainPage, ShellViewModel viewModel, ISolarSystemService solarSystemService)
        {
            BindingContext = viewModel;
            InitializeComponent();
            ShellContent.ContentTemplate = new DataTemplate(() => mainPage);
        }

        public ShellViewModel ViewModel => BindingContext as ShellViewModel ?? throw new ApplicationException($"{nameof(ShellViewModel)} is not initialized");
    }
}
