namespace De.Hochstaetter.FroniusMonitor.Controls
{
    public partial class LinearHorizontalGauge : IHaveDisplayName
    {
        #region Dependency Properties

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register
        (
            nameof(Value), typeof(IFormattable), typeof(LinearHorizontalGauge),
            new PropertyMetadata(double.NaN, (d, _) => ((LinearHorizontalGauge)d).OnValueChanged())
        );

        public IFormattable? Value
        {
            get => (IFormattable?)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register
        (
            nameof(Minimum), typeof(double), typeof(LinearHorizontalGauge),
            new PropertyMetadata((d, _) => ((LinearHorizontalGauge)d).OnValueChanged())
        );

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register
        (
            nameof(Maximum), typeof(double), typeof(LinearHorizontalGauge),
            new PropertyMetadata((d, _) => ((LinearHorizontalGauge)d).OnValueChanged())
        );

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public static readonly DependencyProperty StringFormatProperty = DependencyProperty.Register
        (
            nameof(StringFormat), typeof(string), typeof(LinearHorizontalGauge),
            new PropertyMetadata((d, _) => ((LinearHorizontalGauge)d).OnValueChanged())
        );

        public string? StringFormat
        {
            get => (string?)GetValue(StringFormatProperty);
            set => SetValue(StringFormatProperty, value);
        }

        public static readonly DependencyProperty StringFormatForPercentageProperty = DependencyProperty.Register
        (
            nameof(StringFormatForPercentage), typeof(string), typeof(LinearHorizontalGauge),
            new PropertyMetadata("N2", (d, _) => ((LinearHorizontalGauge)d).OnStringFormatForPercentageChanged())
        );

        public string StringFormatForPercentage
        {
            get => (string)GetValue(StringFormatForPercentageProperty);
            set => SetValue(StringFormatForPercentageProperty, value);
        }

        private void OnStringFormatForPercentageChanged() { }

        public static readonly DependencyProperty UnitSymbolProperty = DependencyProperty.Register
        (
            nameof(UnitSymbol), typeof(string), typeof(LinearHorizontalGauge)
        );

        public string? UnitSymbol
        {
            get => (string?)GetValue(UnitSymbolProperty);
            set => SetValue(UnitSymbolProperty, value);
        }

        public static readonly DependencyProperty DisplayNameProperty = DependencyProperty.Register
        (
            nameof(DisplayName), typeof(string), typeof(LinearHorizontalGauge), new PropertyMetadata(string.Empty)
        );

        public string DisplayName
        {
            get => (string)GetValue(DisplayNameProperty);
            set => SetValue(DisplayNameProperty, value);
        }

        public static readonly DependencyProperty SubDisplayNameProperty = DependencyProperty.Register
        (
            nameof(SubDisplayName), typeof(string), typeof(LinearHorizontalGauge),
            new PropertyMetadata(string.Empty)
        );

        public string SubDisplayName
        {
            get => (string)GetValue(SubDisplayNameProperty);
            set => SetValue(SubDisplayNameProperty, value);
        }

        public static readonly DependencyProperty VeryLowProperty = DependencyProperty.Register
        (
            nameof(VeryLow), typeof(double), typeof(LinearHorizontalGauge),
            new PropertyMetadata(double.MinValue, (d, _) => ((LinearHorizontalGauge)d).OnValueChanged())
        );

        public double VeryLow
        {
            get => (double)GetValue(VeryLowProperty);
            set => SetValue(VeryLowProperty, value);
        }

        public static readonly DependencyProperty LowProperty = DependencyProperty.Register
        (
            nameof(Low), typeof(double), typeof(LinearHorizontalGauge),
            new PropertyMetadata(double.MinValue, (d, _) => ((LinearHorizontalGauge)d).OnValueChanged())
        );

        public double Low
        {
            get => (double)GetValue(LowProperty);
            set => SetValue(LowProperty, value);
        }

        public static readonly DependencyProperty HighProperty = DependencyProperty.Register
        (
            nameof(High), typeof(double), typeof(LinearHorizontalGauge),
            new PropertyMetadata(double.MaxValue, (d, _) => ((LinearHorizontalGauge)d).OnValueChanged())
        );

        public double High
        {
            get => (double)GetValue(HighProperty);
            set => SetValue(HighProperty, value);
        }

        public static readonly DependencyProperty VeryHighProperty = DependencyProperty.Register
        (
            nameof(VeryHigh), typeof(double), typeof(LinearHorizontalGauge),
            new PropertyMetadata(double.MaxValue, (d, _) => ((LinearHorizontalGauge)d).OnValueChanged())
        );

        public double VeryHigh
        {
            get => (double)GetValue(VeryHighProperty);
            set => SetValue(VeryHighProperty, value);
        }

        public static readonly DependencyProperty ProgressBarHeightProperty = DependencyProperty.Register
        (
            nameof(ProgressBarHeight), typeof(double), typeof(LinearHorizontalGauge),
            new PropertyMetadata(17d)
        );

        public double ProgressBarHeight
        {
            get => (double)GetValue(ProgressBarHeightProperty);
            set => SetValue(ProgressBarHeightProperty, value);
        }

        public static readonly DependencyProperty ShowPercentProperty = DependencyProperty.Register
        (
            nameof(ShowPercent), typeof(bool), typeof(LinearHorizontalGauge),
            new PropertyMetadata((d, _) => ((LinearHorizontalGauge)d).OnValueChanged())
        );

        public bool ShowPercent
        {
            get => (bool)GetValue(ShowPercentProperty);
            set => SetValue(ShowPercentProperty, value);
        }

        public static readonly DependencyProperty UseAbsoluteValueProperty = DependencyProperty.Register
        (
            nameof(UseAbsoluteValue), typeof(bool), typeof(LinearHorizontalGauge),
            new PropertyMetadata(true, (d, _) => ((LinearHorizontalGauge)d).OnValueChanged())
        );

        public bool UseAbsoluteValue
        {
            get => (bool)GetValue(UseAbsoluteValueProperty);
            set => SetValue(UseAbsoluteValueProperty, value);
        }

        #endregion

        public LinearHorizontalGauge()
        {
            InitializeComponent();
        }


        private void OnValueChanged()
        {
            var doubleValue = Value as double?;
            var isFinite = doubleValue.HasValue && double.IsFinite(doubleValue.Value);

            ValueTextBlock.Text = Value switch
            {
                double doubleVal => isFinite ? doubleVal.ToString(StringFormat ?? "N0", CultureInfo.CurrentCulture) : "---",
                DateTime dateTimeVal => dateTimeVal.ToLocalTime().ToString(StringFormat ?? "g", CultureInfo.CurrentCulture),
                TimeSpan timeSpanValue => timeSpanValue.ToString(StringFormat ?? "g", CultureInfo.CurrentCulture),
                _ => Value?.ToString(StringFormat, CultureInfo.CurrentCulture) ?? "---",
            };

            if (doubleValue.HasValue)
            {
                if (!isFinite)
                {
                    ValueTextBlock.Text = "---";
                }

                ProgressBar.Value = isFinite ? UseAbsoluteValue ? Math.Abs(doubleValue.Value) : doubleValue.Value : 0;

                ProgressBar.Foreground = ProgressBar.Value < VeryLow || ProgressBar.Value > VeryHigh
                    ? Brushes.Salmon
                    : ProgressBar.Value < Low || ProgressBar.Value > High
                        ? new SolidColorBrush(Color.FromRgb(255, 208, 0))
                        : Brushes.LightGreen;

                var percentage = (ProgressBar.Value - Minimum) / (Maximum - Minimum) * 100;
                PercentRun.Text = percentage.ToString(StringFormatForPercentage, CultureInfo.CurrentCulture);
            }
            else
            {
                ProgressBar.Value = Minimum;
            }
        }
    }
}
