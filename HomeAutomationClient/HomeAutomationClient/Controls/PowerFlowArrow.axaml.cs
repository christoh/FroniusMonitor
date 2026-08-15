namespace De.Hochstaetter.HomeAutomationClient.Controls
{
    public partial class PowerFlowArrow : Viewbox
    {
        public static readonly StyledProperty<IBrush?> FillProperty = AvaloniaProperty.Register<PowerFlowArrow, IBrush?>(nameof(Fill), Brushes.LightGray);

        public IBrush? Fill
        {
            get => GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<PowerFlowArrow, double>(nameof(Value));

        public double Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly StyledProperty<bool> IsDcProperty = AvaloniaProperty.Register<PowerFlowArrow, bool>(nameof(IsDc));

        public bool IsDc
        {
            get => GetValue(IsDcProperty);
            set => SetValue(IsDcProperty, value);
        }

        public static readonly StyledProperty<bool> IsLeftProperty = AvaloniaProperty.Register<PowerFlowArrow, bool>(nameof(IsLeft));

        public bool IsLeft
        {
            get => GetValue(IsLeftProperty);
            set => SetValue(IsLeftProperty, value);
        }

        public static readonly StyledProperty<string> UnitNameProperty = AvaloniaProperty.Register<PowerFlowArrow, string>(nameof(UnitName), "W");

        public string UnitName
        {
            get => GetValue(UnitNameProperty);
            set => SetValue(UnitNameProperty, value);
        }
        
        public static readonly StyledProperty<string> StringFormatProperty=AvaloniaProperty.Register<PowerFlowArrow, string>(nameof(StringFormat), "N1");
        
        public string StringFormat
        {
            get => GetValue(StringFormatProperty);
            set => SetValue(StringFormatProperty, value);
        }

        public PowerFlowArrow()
        {
            InitializeComponent();
        }

        public static readonly DirectProperty<PowerFlowArrow, double> DirectionAngleProperty = AvaloniaProperty.RegisterDirect<PowerFlowArrow, double>(nameof(DirectionAngle), o => o.DirectionAngle);

        public double DirectionAngle
        {
            get;
            set => SetAndRaise(DirectionAngleProperty, ref field, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            switch (e.Property.Name)
            {
                case nameof(Fill):
                    Triangle.Fill = Rectangle.Fill = Fill;
                    break;

                case nameof(Value):
                    SetAngle();
                    SetText();
                    break;

                case nameof(IsLeft):
                    SetAngle();
                    break;

                case nameof(IsDc):
                    AcDc.Text = IsDc ? Loc.Dc : Loc.Ac;
                    break;
            }
        }

        private void SetAngle()
        {
            var isInverted = IsLeft;

            if (Value >= 0)
            {
                isInverted = !isInverted;
            }

            DirectionAngle = isInverted ? 180 : 0;
            ValueStackPanel.Margin = isInverted ? new Thickness(5, 0, 0, 0) : new Thickness(0, 0, 0, 0);

        }

        private void SetText()
        {
            ValueRun.Text = Value switch
            {
                > -.01 and < .01 => "---",
                _ => double.Abs(Value).ToString(StringFormat, CultureInfo.CurrentCulture),
            };
        }
    }
}