namespace De.Hochstaetter.FroniusPhone.Views
{
    public partial class MainView : ContentPage
    {
        int count = 0;

        public MainView()
        {
            InitializeComponent();
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;
            CounterLabel.Text = $"Current count: {count}";

            SemanticScreenReader.Announce(CounterLabel.Text);
        }
    }
}