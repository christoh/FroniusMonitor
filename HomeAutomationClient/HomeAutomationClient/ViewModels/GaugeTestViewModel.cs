namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public partial class GaugeTestViewModel : ViewModelBase
{
    [ObservableProperty] public partial double Value { get; set; } = 1000;

    [ObservableProperty] public partial bool ColorAllTicks { get; set; }

    [ObservableProperty] public partial double Origin { get; set; } = 0d;

    [ObservableProperty] public partial IReadOnlyList<ColorThreshold> GaugeColorScheme { get; set; } = GaugeColors.HighIsBad;

    [ObservableProperty] public partial double Minimum { get; set; } = 1000;

    [ObservableProperty] public partial double Maximum { get; set; } = 1200;
    
    public ICommand? AdjustValueCommand => field ??= new RelayCommand<double>(v => Value += v);
    public ICommand? SetMinimumCommand => field ??= new RelayCommand<string>(v => Minimum = double.Parse(v!, NumberStyles.Float, CultureInfo.InvariantCulture));
    public ICommand? SetMaximumCommand => field ??= new RelayCommand<string>(v => Maximum = double.Parse(v!, NumberStyles.Float, CultureInfo.InvariantCulture));
    public ICommand? SetOriginCommand => field ??= new RelayCommand<string>(v => Origin = double.Parse(v!, NumberStyles.Float, CultureInfo.InvariantCulture));
    public ICommand? SetColorSchemeCommand => field ??= new RelayCommand<IReadOnlyList<ColorThreshold>>(cp => GaugeColorScheme = cp!);
    public ICommand? MinValueCommand => field ??= new RelayCommand(() => Value = Minimum);
    public ICommand? MidValueCommand => field ??= new RelayCommand(() => Value = (Maximum + Minimum) / 2);
    public ICommand? MaxValueCommand => field ??= new RelayCommand(() => Value = Maximum);
    public ICommand? SwitchTickColoringCommand => field ??= new RelayCommand(() => ColorAllTicks = !ColorAllTicks);
}
