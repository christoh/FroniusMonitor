﻿namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class WattPilotSettingsViewModel(IDataCollectionService dataCollectionService, IWattPilotService wattPilotService) : ViewModelBase
{
    private WattPilot oldWattPilot = null!;

    public static IReadOnlyList<CableLockBehavior> CableLockBehaviors { get; } = Enum.GetValues<CableLockBehavior>();
    public static IReadOnlyList<ChargingLogic> ChargingLogicList { get; } = Enum.GetValues<ChargingLogic>();
    public static IReadOnlyList<EcoRoundingMode> EcoRoundingModes { get; } = Enum.GetValues<EcoRoundingMode>();
    public static IReadOnlyList<PhaseSwitchMode> PhaseSwitchModes { get; } = Enum.GetValues<PhaseSwitchMode>();
    public static IReadOnlyList<AwattarCountry> EnergyPriceCountries { get; } = Enum.GetValues<AwattarCountry>().OrderBy(c => c.ToDisplayName()).ToArray();
    public static IReadOnlyList<ForcedCharge> ForcedChargeList { get; } = Enum.GetValues<ForcedCharge>().OrderBy(c => c.ToDisplayName()).ToArray();
    public static IReadOnlyList<LoadBalancingPriority> LoadBalancingPriorities { get; } = Enum.GetValues<LoadBalancingPriority>();

    public WattPilot WattPilot
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(ApiLink));
            NotifyOfPropertyChange(nameof(ApiUri));
        });
    } = null!;

    public bool EnableDanger
    {
        get;
        set => Set(ref field, value);
    }

    public string ToastText
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    public string NextTripTimeString
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    public string Title
    {
        get;
        set => Set(ref field, value);
    } = "Wattpilot";

    public float NextTripEnergyToCharge
    {
        get;

        set => Set(ref field, value, () =>
        {
            if (WattPilot != null!)
            {
                WattPilot.NextTripEnergyToCharge = (int)Math.Round(value * 1000, MidpointRounding.AwayFromZero);
            }
        });
    }

    public bool IsInUpdate
    {
        get;
        set => Set(ref field, value);
    }

    public string WattPilotWifiPassword
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    public bool RequiresChargingInterval
    {
        get;
        set => Set(ref field, value, () =>
        {
            if (value && WattPilot.MinimumChargingInterval < 300000)
            {
                WattPilot.MinimumChargingInterval = 300000;
            }
        });
    }

    public bool IsUserAuthenticated
    {
        get;
        set => Set(ref field, value);
    }

    public string ApiLink => "https://" + (WattPilot.SerialNumber ?? "<Serial>") + ".api.v3.go-e.io";
    public string ApiUri => ApiLink + $"/api/status?token={WattPilot.CloudAccessKey ?? "<Token>"}";

    [field: AllowNull, MaybeNull]
    public ICommand ApplyCommand => field ??= new NoParameterCommand(Apply);

    [field: AllowNull, MaybeNull]
    public ICommand UndoCommand => field ??= new NoParameterCommand(() => Undo());

    [field: AllowNull, MaybeNull]
    public ICommand NavigateToApiCommand => field ??= new NoParameterCommand(NavigateToApi);

    private void NavigateToApi()
    {
        Process.Start(new ProcessStartInfo(ApiUri) { UseShellExecute = true });
    }


    internal override async Task OnInitialize()
    {
        await base.OnInitialize().ConfigureAwait(false);
        var localWattPilot = wattPilotService.WattPilot ?? dataCollectionService.HomeAutomationSystem?.WattPilot;

        if (localWattPilot == null)
        {
            ShowBox(Loc.NoWattPilot, Loc.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
            return;
        }

        IsUserAuthenticated = localWattPilot.AuthenticatedCardIndex.HasValue;
        oldWattPilot = (WattPilot)localWattPilot.Clone();

        if (oldWattPilot.NextTripTime.HasValue)
        {
            var timeSpan = TimeSpan.FromSeconds(oldWattPilot.NextTripTime.Value);
            NextTripTimeString = $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}";
        }

        if (oldWattPilot.NextTripEnergyToCharge.HasValue)
        {
            NextTripEnergyToCharge = oldWattPilot.NextTripEnergyToCharge.Value / 1000f;
        }

        Undo();
    }

    private void Undo(bool resetWriteOnlyFields = true)
    {
        if (resetWriteOnlyFields)
        {
            WattPilotWifiPassword = string.Empty;
        }

        WattPilot = (WattPilot)oldWattPilot.Clone();

        Title = $"{WattPilot.DeviceName} {WattPilot.SerialNumber}";
        RequiresChargingInterval = (WattPilot.MinimumChargingInterval ?? 0) != 0;

        if (WattPilot.AllowChargingPause.HasValue && !WattPilot.AllowChargingPause.Value)
        {
            WattPilot.PhaseSwitchMode = PhaseSwitchMode.Phases3;
        }
    }

    private async void Apply()
    {
        if (WattPilot.LoadBalancingCurrents is not null && WattPilot.LoadBalancingCurrents != oldWattPilot.LoadBalancingCurrents)
        {
            WattPilot.LoadBalancingCurrents.TimeStamp = DateTime.UtcNow;
        }

        try
        {
            foreach (var error in NotifiedValidationErrors)
            {
                if
                (
                    error.BindingInError is BindingExpression { Target: FrameworkElement { IsVisible: false } } expression &&
                    oldWattPilot.GetType().GetProperty(expression.ResolvedSourcePropertyName) is { } property
                )
                {
                    var value = property.GetValue(oldWattPilot);
                    property.SetValue(WattPilot, value);
                }
            }

            IList<string> errors = NotifiedValidationErrors
                .Where(e => e.BindingInError is BindingExpression { Target: FrameworkElement { IsVisible: true } })
                .Select(e => e.ErrorContent.ToString() ?? string.Empty).ToList();

            if (errors.Count > 0)
            {
                ShowBox
                (
                    $"{Loc.PleaseCorrectErrors}:{Environment.NewLine}{errors.Aggregate(string.Empty, (c, n) => c + Environment.NewLine + "• " + n)}",
                    Loc.Error, MessageBoxButton.OK, MessageBoxImage.Error
                );

                return;
            }

            IsInUpdate = true;

            var nextTripTime = Gen24ChargingRule.GetDate(NextTripTimeString);

            if (nextTripTime.HasValue)
            {
                WattPilot.NextTripTime = Math.Min(nextTripTime.Value.Hour * 3600 + nextTripTime.Value.Minute * 60, 86399);
            }

            if (IsUserAuthenticated)
            {
                WattPilot.AuthenticatedCardIndex ??= 0;
            }
            else
            {
                WattPilot.AuthenticatedCardIndex = null;
            }

            if (!RequiresChargingInterval)
            {
                WattPilot.MinimumChargingInterval = 0;
            }

            if (WattPilot.AllowChargingPause.HasValue && !WattPilot.AllowChargingPause.Value)
            {
                WattPilot.PhaseSwitchMode = PhaseSwitchMode.Phases3;
            }

            if (!string.IsNullOrWhiteSpace(WattPilotWifiPassword))
            {
                WattPilot.WifiPassword = WattPilotWifiPassword;
            }

            wattPilotService.BeginSendValues();
            var sentSomething = false;

            try
            {
                errors = await wattPilotService.Send(WattPilot, oldWattPilot).ConfigureAwait(false);
                sentSomething = true;

                if (errors.Count > 0)
                {
                    var notWritten = "• " + string.Join(Environment.NewLine + "• ", errors);

                    ShowBox
                    (
                        "The following settings were not written to the Wattpilot:" + Environment.NewLine + Environment.NewLine + notWritten,
                        Loc.Error, MessageBoxButton.OK, MessageBoxImage.Error
                    );
                }
            }
            catch (ArgumentException ex)
            {
                sentSomething = false;
                IsInUpdate = false;
                ShowBox(ex.Message, Loc.Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                try
                {
                    await wattPilotService.WaitSendValues().ConfigureAwait(false);

                    if (sentSomething)
                    {
                        ToastText = Loc.SentToWattPilot;
                    }

                    oldWattPilot = WattPilot;
                    Undo(false);
                }
                catch (TimeoutException) when (wattPilotService.UnsuccessfulWrites.Count > 0)
                {
                    var notWritten = "• " + string.Join(Environment.NewLine + "• ", wattPilotService.UnsuccessfulWrites.Select(a => a.ToString()));
                    IsInUpdate = false;

                    ShowBox
                    (
                        "The following settings were not confirmed by the Wattpilot:" + Environment.NewLine + Environment.NewLine + notWritten,
                        Loc.Warning, MessageBoxButton.OK, MessageBoxImage.Warning
                    );
                }
            }
        }
        finally
        {
            IsInUpdate = false;
        }
    }
}
