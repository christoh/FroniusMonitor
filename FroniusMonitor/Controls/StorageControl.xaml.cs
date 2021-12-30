﻿using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using De.Hochstaetter.Fronius.Models;

namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class StorageControl
{
    private bool isInChargingAnimation;

    private static readonly ColorAnimation chargingAnimation = new()
    {
        To = Color.FromRgb(0, 136, 178),
        AutoReverse = true,
        RepeatBehavior = RepeatBehavior.Forever,
        Duration = TimeSpan.FromSeconds(1.5),
    };

    public static readonly DependencyProperty StorageProperty = DependencyProperty.Register
    (
        nameof(Storage), typeof(Storage), typeof(StorageControl),
        new PropertyMetadata((d, e) => ((StorageControl)d).OnStorageChanged(e))
    );

    public Storage? Storage
    {
        get => (Storage?)GetValue(StorageProperty);
        set => SetValue(StorageProperty, value);
    }

    public StorageControl()
    {
        InitializeComponent();

        Unloaded += (_, _) =>
        {
            if (Storage != null)
            {
                Storage.PropertyChanged -= OnDataChanged;
            }
        };
    }

    private void OnStorageChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is Storage oldStorage)
        {
            oldStorage.PropertyChanged -= OnDataChanged;
        }

        if (Storage != null)
        {
            Storage.PropertyChanged += OnDataChanged;
        }

        OnDataChanged();
    }



    private void OnDataChanged(object? _ = null, PropertyChangedEventArgs? __ = null)
    {
        Dispatcher.InvokeAsync(() =>
        {
            var data = Storage?.Data;

            if (data == null)
            {
                return;
            }

            SocRectangle.Height = (data.StateOfCharge ?? 0) * BackgroundRectangle.Height;

            if (data.Power > 10)
            {
                if (!isInChargingAnimation)
                {
                    isInChargingAnimation = true;
                    PlusPole.Background = Enclosure.BorderBrush = new SolidColorBrush(Colors.DarkGreen);
                    PlusPole.Background.BeginAnimation(SolidColorBrush.ColorProperty, chargingAnimation);
                }
            }
            else
            {
                if (!PlusPole.Background.IsFrozen)
                {
                    PlusPole.Background.BeginAnimation(SolidColorBrush.ColorProperty, null);
                }

                var brush = data.TrafficLight switch
                {
                    TrafficLight.Red => Brushes.Red,
                    TrafficLight.Green => Brushes.DarkGreen,
                    _ => Brushes.DarkGray
                };

                isInChargingAnimation = false;
                PlusPole.Background = Enclosure.BorderBrush = brush;
            }
        });
    }
}