namespace De.Hochstaetter.FroniusMonitor.Assets.Images
{
    /// <summary>
    ///     Interaction logic for VisibilityIcon.xaml
    /// </summary>
    public partial class VisibilityIcon
    {
        public static readonly DependencyProperty VisibleProperty = DependencyProperty.Register
        (
            nameof(Visible), typeof(bool), typeof(VisibilityIcon)
        );

        public bool Visible
        {
            get => (bool)GetValue(VisibleProperty);
            set => SetValue(VisibleProperty, value);
        }


        public VisibilityIcon()
        {
            InitializeComponent();
        }
    }
}
