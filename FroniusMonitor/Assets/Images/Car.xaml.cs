﻿using System.Windows.Shapes;

namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class Car
{
    private readonly ColorAnimation animation=new ColorAnimation(Colors.Transparent,Colors.Transparent,TimeSpan.FromSeconds(2)){AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, AccelerationRatio = .25, DecelerationRatio = .25};

    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register
    (
        nameof(Status), typeof(CarStatus?), typeof(Car),
        new PropertyMetadata((d, _) => ((Car)d).OnCarStatusChanged())
    );

    public CarStatus? Status
    {
        get => (CarStatus?)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public Car()
    {
        InitializeComponent();
    }

    private void OnCarStatusChanged()
    {
        switch (Status)
        {
            case CarStatus.Charging:
                animation.From = Colors.Blue;
                animation.To = Colors.DodgerBlue;
                animation.Duration = TimeSpan.FromSeconds(1.5);
                break;

            case CarStatus.Error:
                animation.From = Colors.DarkRed;
                animation.To = Colors.OrangeRed;
                animation.Duration = TimeSpan.FromSeconds(.5);
                break;

            case CarStatus.Complete:
                animation.From = Colors.Green;
                animation.To = Colors.Green;
                animation.Duration = TimeSpan.FromSeconds(20);
                break;

            case CarStatus.WaitCar:
                animation.From = Colors.Yellow;
                animation.To = Colors.DarkOrange;
                animation.Duration = TimeSpan.FromSeconds(1.5);
                break;

            default:
                animation.From = Colors.LightGray;
                animation.To = Colors.LightGray;
                animation.Duration = TimeSpan.FromSeconds(20);
                break;
        }

        var brush = new SolidColorBrush(animation.From??Colors.Transparent);

        CarShape.Fill = brush;
        CarShape.Fill.BeginAnimation(SolidColorBrush.ColorProperty,animation);
    }
}

public class CarExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new Car();
}
