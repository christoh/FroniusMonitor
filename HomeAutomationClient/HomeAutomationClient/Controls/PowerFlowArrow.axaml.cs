using Avalonia.Layout;

namespace De.Hochstaetter.HomeAutomationClient.Controls
{
    public partial class PowerFlowArrow : ContentControl
    {
        public static readonly StyledProperty<bool> IsVerticalProperty = AvaloniaProperty.Register<PowerFlowArrow, bool>(nameof(IsVertical));

        public bool IsVertical
        {
            get => GetValue(IsVerticalProperty);
            set => SetValue(IsVerticalProperty, value);
        }

        public static readonly StyledProperty<double> RotationDegreesProperty = AvaloniaProperty.Register<PowerFlowArrow, double>(nameof(RotationDegrees));

        public double RotationDegrees
        {
            get => GetValue(RotationDegreesProperty);
            set => SetValue(RotationDegreesProperty, value);
        }

        public static readonly StyledProperty<IBrush?> FillProperty = AvaloniaProperty.Register<PowerFlowArrow, IBrush?>(nameof(Fill), Brushes.LightGray);

        public IBrush? Fill
        {
            get => GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        public static readonly StyledProperty<double?> ValueProperty = AvaloniaProperty.Register<PowerFlowArrow, double?>(nameof(Value));

        public double? Value
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

        public static readonly StyledProperty<bool> IsLeftProperty = AvaloniaProperty.Register<PowerFlowArrow, bool>(nameof(InvertArrow));

        public bool InvertArrow
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

        public static readonly StyledProperty<string> StringFormatProperty = AvaloniaProperty.Register<PowerFlowArrow, string>(nameof(StringFormat), "N1");

        public string StringFormat
        {
            get => GetValue(StringFormatProperty);
            set => SetValue(StringFormatProperty, value);
        }

        public PowerFlowArrow()
        {
            InitializeComponent();
            SetIsVertical();
            SetText();
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
                    SetFill();
                    break;

                case nameof(Value):
                    SetAngle();
                    SetText();
                    break;

                case nameof(InvertArrow):
                    SetAngle();
                    break;

                case nameof(IsDc):
                    AcDc.Text = IsDc ? Loc.Dc : Loc.Ac;
                    break;

                case nameof(IsVertical):
                    SetIsVertical();
                    break;
            }
        }

        private void SetFill()
        {
            Arrow.Fill = Fill;

            if (Fill is ISolidColorBrush { Color: var color })
            {
                var useBlackText = color.R + color.G + color.B > 382;
                TextBlock.Foreground = AcDc.Foreground = useBlackText ? Brushes.Black : Brushes.White;
            }
        }

        private void SetIsVertical()
        {
            RootElement.Width = IsVertical ? 85 : 70;
            RootElement.Height = IsVertical ? 50 : 30;
            ValueStackPanel.HorizontalAlignment = IsVertical ? HorizontalAlignment.Left : HorizontalAlignment.Center;
            TextBlock.Margin = IsVertical ? new Thickness(30, 0, 0, 0) : new Thickness(0, 0, 0, 0);
            SetAngle();
        }

        private void SetAngle()
        {
            var isInverted = InvertArrow;

            if (Value >= 0)
            {
                isInverted = !isInverted;
            }

            DirectionAngle = isInverted ? 180 : 0;
            ValueStackPanel.Margin = isInverted && !IsVertical ? new Thickness(5, 0, 0, 0) : new Thickness(0, 0, 0, 0);

        }

        private void SetText()
        {
            ValueRun.Text = Value switch
            {
                null or > -.01 and < .01 => "---",
                _ => double.Abs(Value.Value).ToString(StringFormat, CultureInfo.CurrentCulture),
            };
        }
    }
}