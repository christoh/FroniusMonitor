using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Greeting { get; set; } = "Home Automation Control Center Alpha Test";

    [ObservableProperty]
    public partial double Value { get; set; } = 1000;

    public ICommand? AdjustValueCommand => field ??= new RelayCommand<double>(v => Value += v);
    public ICommand? MinValueCommand => field ??= new RelayCommand(() => Value = 1000);
    public ICommand? MidValueCommand => field ??= new RelayCommand(() => Value = 1100);
    public ICommand? MaxValueCommand => field ??= new RelayCommand(() => Value = 1200);
}