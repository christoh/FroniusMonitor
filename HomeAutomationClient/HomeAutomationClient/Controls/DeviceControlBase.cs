namespace De.Hochstaetter.HomeAutomationClient.Controls;

public abstract class DeviceControlBase : UserControl
{
    public static readonly StyledProperty<IBrush?> InnerRunningProperty = AvaloniaProperty.Register<DeviceControlBase, IBrush?>(nameof(InnerRunning));
    public static readonly StyledProperty<IBrush?> InnerFaultProperty = AvaloniaProperty.Register<DeviceControlBase, IBrush?>(nameof(InnerFault));
    public static readonly StyledProperty<IBrush?> InnerWarningProperty = AvaloniaProperty.Register<DeviceControlBase, IBrush?>(nameof(InnerWarning));
    public static readonly StyledProperty<IBrush?> InnerStartupProperty = AvaloniaProperty.Register<DeviceControlBase, IBrush?>(nameof(InnerStartup));
    public static readonly StyledProperty<IBrush?> InnerOtherProperty = AvaloniaProperty.Register<DeviceControlBase, IBrush?>(nameof(InnerOther));

    public static readonly StyledProperty<IBrush?> OuterRunningProperty = AvaloniaProperty.Register<DeviceControlBase, IBrush?>(nameof(OuterRunning));
    public static readonly StyledProperty<IBrush?> OuterFaultProperty = AvaloniaProperty.Register<DeviceControlBase, IBrush?>(nameof(OuterFault));
    public static readonly StyledProperty<IBrush?> OuterWarningProperty = AvaloniaProperty.Register<DeviceControlBase, IBrush?>(nameof(OuterWarning));
    public static readonly StyledProperty<IBrush?> OuterStartupProperty = AvaloniaProperty.Register<DeviceControlBase, IBrush?>(nameof(OuterStartup));
    public static readonly StyledProperty<IBrush?> OuterOtherProperty = AvaloniaProperty.Register<DeviceControlBase, IBrush?>(nameof(OuterOther));

    public static readonly StyledProperty<IBrush?> LcdBackgroundProperty = AvaloniaProperty.Register<DeviceControlBase, IBrush?>(nameof(LcdBackground));
    public static readonly StyledProperty<IBrush?> CleaningBackgroundProperty = AvaloniaProperty.Register<DeviceControlBase, IBrush?>(nameof(CleaningBackground));
    
    public IBrush? CleaningBackground
    {
        get => GetValue(CleaningBackgroundProperty);
        set => SetValue(CleaningBackgroundProperty, value);
    }

    public IBrush? InnerRunning
    {
        get => GetValue(InnerRunningProperty);
        set => SetValue(InnerRunningProperty, value);
    }

    public IBrush? InnerFault
    {
        get => GetValue(InnerFaultProperty);
        set => SetValue(InnerFaultProperty, value);
    }

    public IBrush? InnerWarning
    {
        get => GetValue(InnerWarningProperty);
        set => SetValue(InnerWarningProperty, value);
    }

    public IBrush? InnerStartup
    {
        get => GetValue(InnerStartupProperty);
        set => SetValue(InnerStartupProperty, value);
    }

    public IBrush? InnerOther
    {
        get => GetValue(InnerOtherProperty);
        set => SetValue(InnerOtherProperty, value);
    }

    public IBrush? OuterRunning
    {
        get => GetValue(OuterRunningProperty);
        set => SetValue(OuterRunningProperty, value);
    }

    public IBrush? OuterFault
    {
        get => GetValue(OuterFaultProperty);
        set => SetValue(OuterFaultProperty, value);
    }

    public IBrush? OuterWarning
    {
        get => GetValue(OuterWarningProperty);
        set => SetValue(OuterWarningProperty, value);
    }

    public IBrush? OuterStartup
    {
        get => GetValue(OuterStartupProperty);
        set => SetValue(OuterStartupProperty, value);
    }

    public IBrush? OuterOther
    {
        get => GetValue(OuterOtherProperty);
        set => SetValue(OuterOtherProperty, value);
    }

    public IBrush? LcdBackground
    {
        get => GetValue(LcdBackgroundProperty);
        set => SetValue(LcdBackgroundProperty, value);
    }
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        switch (change.Property.Name)
        {
            case nameof(OuterFault):
            case nameof(OuterRunning):
            case nameof(OuterOther):
            case nameof(OuterStartup):
            case nameof(OuterWarning):
            case nameof(CleaningBackground):
                ChangeOuter();
                break;
            
            case nameof(InnerFault):
            case nameof(InnerRunning):
            case nameof(InnerOther):
            case nameof(InnerStartup):
            case nameof(InnerWarning):
            case nameof(LcdBackground):
                ChangeInner();
                break;
        }
    }

    protected abstract void ChangeInner();
    protected abstract void ChangeOuter();
}