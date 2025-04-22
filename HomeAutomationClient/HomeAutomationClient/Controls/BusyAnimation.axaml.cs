namespace De.Hochstaetter.HomeAutomationClient.Controls;

public partial class BusyAnimation : UserControl
{
    public static readonly StyledProperty<string?> BusyTextProperty = AvaloniaProperty.Register<BusyAnimation, string?>(nameof(BusyText));

    public string? BusyText
    {
        get => GetValue(BusyTextProperty);
        set => SetValue(BusyTextProperty, value);
    }

    public static readonly DirectProperty<BusyAnimation, bool> IsBusyProperty = AvaloniaProperty.RegisterDirect<BusyAnimation, bool>(nameof(IsBusy), o => o.IsBusy);

    public bool IsBusy
    {
        get; private set => SetAndRaise(IsBusyProperty, ref field, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        switch (change.Property.Name)
        {
            case nameof(BusyText):
                IsBusy = BusyText != null;
                break;
        }
    }

    public BusyAnimation()
    {
        InitializeComponent();
    }


}