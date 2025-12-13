namespace De.Hochstaetter.HomeAutomationClient.Controls
{
    public partial class WifiControl : Viewbox
    {
        public static readonly StyledProperty<IBrush?> FillProperty = Shape.FillProperty.AddOwner<WifiControl>();

        public IBrush? Fill
        {
            get => GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        public static readonly StyledProperty<double> SignalStrengthProperty = AvaloniaProperty.Register<WifiControl, double>(nameof(SignalStrength), -50d);
        //new PropertyMetadata(-50d, (d, _) => ((WifiControl)d).OnSignalStrengthChanged())

        public double SignalStrength
        {
            get => GetValue(SignalStrengthProperty);
            set => SetValue(SignalStrengthProperty, value);
        }

        public static readonly StyledProperty<bool> IsConnectedProperty = AvaloniaProperty.Register<WifiControl, bool>(nameof(IsConnected), true);

        public bool IsConnected
        {
            get => GetValue(IsConnectedProperty);
            set => SetValue(IsConnectedProperty, value);
        }

        public WifiControl()
        {
            InitializeComponent();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            try
            {
                switch (change.Property.Name)
                {
                    case nameof(SignalStrength):
                        High.Opacity = !IsConnected || SignalStrength < -52 ? 0.3 : 1;
                        Medium.Opacity = !IsConnected || SignalStrength < -58 ? 0.3 : 1;
                        Low.Opacity = !IsConnected || SignalStrength < -65 ? 0.3 : 1;
                        break;

                    case nameof(IsConnected):
                        Connection.Opacity = !IsConnected ? 0.3 : 1;
                        break;
                }
            }
            finally
            {
                base.OnPropertyChanged(change);
            }
        }
    }
}
