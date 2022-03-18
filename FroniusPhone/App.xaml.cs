using De.Hochstaetter.FroniusPhone.Views;

namespace De.Hochstaetter.FroniusPhone;

public partial class App : Application
{
    public App(MainView mainPage)
    {
        InitializeComponent();
        MainPage = mainPage;
    }
}
