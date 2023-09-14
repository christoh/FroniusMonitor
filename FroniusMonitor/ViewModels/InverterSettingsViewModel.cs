namespace De.Hochstaetter.FroniusMonitor.ViewModels
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public class InverterSettingsViewModel : SettingsViewModelBase
    {
        private Gen24InverterSettings oldSettings = null!;

        public InverterSettingsViewModel(IDataCollectionService dataCollectionService, IWebClientService webClientService, IGen24JsonService gen24Service, IWattPilotService wattPilotService) : base(dataCollectionService, webClientService, gen24Service, wattPilotService) { }

        private ICommand? undoCommand;
        public ICommand UndoCommand => undoCommand ??= new NoParameterCommand(Undo);

        private ICommand? applyCommand;
        public ICommand ApplyCommand => applyCommand ??= new NoParameterCommand(Apply);

        private IEnumerable<ListItemModel<MpptPowerMode>> powerModes = null!;

        public IEnumerable<ListItemModel<MpptPowerMode>> PowerModes
        {
            get => powerModes;
            set => Set(ref powerModes, value);
        }

        private ListItemModel<MpptPowerMode>? selectedPowerModeMppt1;

        public ListItemModel<MpptPowerMode>? SelectedPowerModeMppt1
        {
            get => selectedPowerModeMppt1;
            set => Set(ref selectedPowerModeMppt1, value, () =>
            {
                if (Settings.Mppt?.Mppt1 != null)
                {
                    Settings.Mppt.Mppt1.PowerMode = value?.Value;
                }
            });
        }

        private ListItemModel<MpptPowerMode>? selectedPowerModeMppt2;

        public ListItemModel<MpptPowerMode>? SelectedPowerModeMppt2
        {
            get => selectedPowerModeMppt2;
            set => Set(ref selectedPowerModeMppt2, value, () =>
            {
                if (Settings.Mppt?.Mppt2 != null)
                {
                    Settings.Mppt.Mppt2.PowerMode = value?.Value;
                }
            });
        }

        private IEnumerable<ListItemModel<MpptOnOff>> dynamicPeakManagerModes = null!;

        public IEnumerable<ListItemModel<MpptOnOff>> DynamicPeakManagerModes
        {
            get => dynamicPeakManagerModes;
            set => Set(ref dynamicPeakManagerModes, value);
        }

        private ListItemModel<MpptOnOff>? selectedDynamicPeakManagerModeMppt1;

        public ListItemModel<MpptOnOff>? SelectedDynamicPeakManagerModeMppt1
        {
            get => selectedDynamicPeakManagerModeMppt1;
            set => Set(ref selectedDynamicPeakManagerModeMppt1, value, () =>
            {
                if (Settings.Mppt?.Mppt1 != null)
                {
                    Settings.Mppt.Mppt1.DynamicPeakManager = value?.Value;
                }
            });
        }

        private ListItemModel<MpptOnOff>? selectedDynamicPeakManagerModeMppt2;

        public ListItemModel<MpptOnOff>? SelectedDynamicPeakManagerModeMppt2
        {
            get => selectedDynamicPeakManagerModeMppt2;
            set => Set(ref selectedDynamicPeakManagerModeMppt2, value, () =>
            {
                if (Settings.Mppt?.Mppt2 != null)
                {
                    Settings.Mppt.Mppt2.DynamicPeakManager = value?.Value;
                }
            });
        }

        private IEnumerable<ListItemModel<PowerLimitMode>> powerLimitModes = null!;

        public IEnumerable<ListItemModel<PowerLimitMode>> PowerLimitModes
        {
            get => powerLimitModes;
            set => Set(ref powerLimitModes, value);
        }

        private ListItemModel<PowerLimitMode> selectedPowerLimitMode = null!;

        public ListItemModel<PowerLimitMode> SelectedPowerLimitMode
        {
            get => selectedPowerLimitMode;
            set => Set(ref selectedPowerLimitMode, value, () => Settings.PowerLimitSettings.ExportLimits.ActivePower.PowerLimitMode = value.Value);
        }

        private string title = Loc.InverterSettings;

        public string Title
        {
            get => title;
            set => Set(ref title, value);
        }

        private Gen24InverterSettings settings = null!;

        public Gen24InverterSettings Settings
        {
            get => settings;
            set => Set(ref settings, value);
        }

        private double logWattPeakMppt1;

        public double LogWattPeakMppt1
        {
            get => logWattPeakMppt1;
            set => Set(ref logWattPeakMppt1, value, UpdateWattPeakMppt1);
        }

        private uint wattPeakMppt1;

        public uint WattPeakMppt1
        {
            get => wattPeakMppt1;
            set => Set(ref wattPeakMppt1, value, UpdateLogWattPeakMppt1);
        }

        private double logWattPeakMppt2;

        public double LogWattPeakMppt2
        {
            get => logWattPeakMppt2;
            set => Set(ref logWattPeakMppt2, value, UpdateWattPeakMppt2);
        }

        private uint wattPeakMppt2;

        public uint WattPeakMppt2
        {
            get => wattPeakMppt2;
            set => Set(ref wattPeakMppt2, value, UpdateLogWattPeakMppt2);
        }

        private bool enableDanger;

        public bool EnableDanger
        {
            get => enableDanger;
            set => Set(ref enableDanger, value);
        }

        private double softLimit;

        public double SoftLimit
        {
            get => softLimit;
            set => Set(ref softLimit, value, () =>
            {
                Settings.PowerLimitSettings.ExportLimits.ActivePower.SoftLimit.PowerLimit = value;
                HardLimit = Math.Max(HardLimit, value);
                WattPeakReferenceValue = Math.Max(WattPeakReferenceValue, HardLimit);
            });
        }

        private double hardLimit;

        public double HardLimit
        {
            get => hardLimit;
            set => Set(ref hardLimit, value, () =>
            {
                Settings.PowerLimitSettings.ExportLimits.ActivePower.HardLimit.PowerLimit = value;
                SoftLimit = Math.Min(SoftLimit, value);
                WattPeakReferenceValue = Math.Max(WattPeakReferenceValue, value);
            });
        }

        private double wattPeakReferenceValue;

        public double WattPeakReferenceValue
        {
            get => wattPeakReferenceValue;
            set => Set(ref wattPeakReferenceValue, value, () =>
            {
                Settings.PowerLimitSettings.Visualization.WattPeakReferenceValue = value;
                HardLimit = Math.Min(HardLimit, value);
                SoftLimit = Math.Min(SoftLimit, HardLimit);
            });
        }

        internal override async Task OnInitialize()
        {
            IsInUpdate = true;

            try
            {
                await base.OnInitialize().ConfigureAwait(false);
                oldSettings = await ReadDataFromInverter().ConfigureAwait(false);

                PowerModes = new[]
                {
                    new ListItemModel<MpptPowerMode> { Value = MpptPowerMode.Off, DisplayName = await WebClientService.GetFroniusName(MpptPowerMode.Off).ConfigureAwait(false) },
                    new ListItemModel<MpptPowerMode> { Value = MpptPowerMode.Auto, DisplayName = await WebClientService.GetFroniusName(MpptPowerMode.Auto).ConfigureAwait(false) },
                    new ListItemModel<MpptPowerMode> { Value = MpptPowerMode.Fix, DisplayName = await WebClientService.GetFroniusName(MpptPowerMode.Fix).ConfigureAwait(false) },
                };

                DynamicPeakManagerModes = new[]
                {
                    new ListItemModel<MpptOnOff> { Value = MpptOnOff.Off, DisplayName = await WebClientService.GetFroniusName(MpptOnOff.Off).ConfigureAwait(false) },
                    new ListItemModel<MpptOnOff> { Value = MpptOnOff.On, DisplayName = await WebClientService.GetFroniusName(MpptOnOff.On).ConfigureAwait(false) },
                    new ListItemModel<MpptOnOff> { Value = MpptOnOff.OnMlsd, DisplayName = await WebClientService.GetFroniusName(MpptOnOff.OnMlsd).ConfigureAwait(false) },
                };

                PowerLimitModes = new[]
                {
                    new ListItemModel<PowerLimitMode> { Value = PowerLimitMode.Off, DisplayName = await WebClientService.GetFroniusName(PowerLimitMode.Off).ConfigureAwait(false) },
                    new ListItemModel<PowerLimitMode> { Value = PowerLimitMode.EntireSystem, DisplayName = await WebClientService.GetConfigString("EXPORTLIMIT.WLIM_MAX_W").ConfigureAwait(false) },
                    new ListItemModel<PowerLimitMode> { Value = PowerLimitMode.WeakestPhase, DisplayName = await WebClientService.GetConfigString("EXPORTLIMIT.WLIM_MAX_FEEDIN_PER_PHASE").ConfigureAwait(false) },
                };

                Undo();
            }
            finally
            {
                IsInUpdate = false;
            }
        }

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private async void Apply()
        {
            IsInUpdate = true;

            try
            {
                var hasUpdates = false;
                await UpdateIfRequired(Settings, oldSettings, "config/common").ConfigureAwait(false);
                await UpdateIfRequired(Settings.Mppt?.Mppt1, oldSettings.Mppt?.Mppt1, "config/setup/powerunit/mppt/mppt1").ConfigureAwait(false);
                await UpdateIfRequired(Settings.Mppt?.Mppt2, oldSettings.Mppt?.Mppt2, "config/setup/powerunit/mppt/mppt2").ConfigureAwait(false);

                if (Settings.PowerLimitSettings.ExportLimits.ActivePower.PowerLimitMode == PowerLimitMode.Off)
                {
                    Settings.PowerLimitSettings = new();
                }
                else
                {
                    SetLimit(Settings.PowerLimitSettings.ExportLimits.ActivePower.HardLimit);
                    SetLimit(Settings.PowerLimitSettings.ExportLimits.ActivePower.SoftLimit);

                    if
                    (
                        !Settings.PowerLimitSettings.ExportLimits.ActivePower.HardLimit.IsEnabled &&
                        !Settings.PowerLimitSettings.ExportLimits.ActivePower.SoftLimit.IsEnabled
                    )
                    {
                        Settings.PowerLimitSettings = new();
                    }
                }

                var visualizationToken = Gen24Service.GetUpdateToken(Settings.PowerLimitSettings.Visualization, oldSettings.PowerLimitSettings.Visualization);
                visualizationToken.Add("exportLimits", new JObject { { "activePower", new JObject { { "displayModeSoftLimit", "absolute" } } }, });

                var hardLimitToken = Gen24Service.GetUpdateToken(Settings.PowerLimitSettings.ExportLimits.ActivePower.HardLimit, oldSettings.PowerLimitSettings.ExportLimits.ActivePower.HardLimit);
                var softLimitToken = Gen24Service.GetUpdateToken(Settings.PowerLimitSettings.ExportLimits.ActivePower.SoftLimit, oldSettings.PowerLimitSettings.ExportLimits.ActivePower.SoftLimit);

                var activePowerToken = Gen24Service.GetUpdateToken(Settings.PowerLimitSettings.ExportLimits.ActivePower, oldSettings.PowerLimitSettings.ExportLimits.ActivePower);
                activePowerToken.Add("hardLimit", hardLimitToken);
                activePowerToken.Add("softLimit", softLimitToken);

                var exportLimitsToken = Gen24Service.GetUpdateToken(Settings.PowerLimitSettings.ExportLimits, oldSettings.PowerLimitSettings.ExportLimits);
                exportLimitsToken.Add("activePower", activePowerToken);

                var limitsToken = new JObject
                {
                    { "visualization", visualizationToken },
                    { "exportLimits", exportLimitsToken },
                };

                if (new[] { visualizationToken, hardLimitToken, softLimitToken, activePowerToken, exportLimitsToken, limitsToken }
                    .Any(t => t.Children().Any(c => c is JProperty p && p.Value.Children().Any(v => v.Children().Any(child => child is JValue)))))
                {
                    hasUpdates = true;
                }

                if (!hasUpdates)
                {
                    ShowNoSettingsChanged();
                    return;
                }

                var success = await UpdateInverter("config/powerlimits", limitsToken).ConfigureAwait(false);

                oldSettings = Settings;
                Undo();
                ToastText = Loc.SettingsSavedToInverter;
                return;

                static void SetLimit(Gen24PowerLimitDefinition limit)
                {
                    if (!limit.IsEnabled)
                    {
                        limit.PowerLimit = 0;
                    }
                }

                async ValueTask UpdateIfRequired<T>(T? newValues, T? oldValues, string uri) where T : BindableBase
                {
                    if (newValues is not null && oldValues is not null)
                    {
                        var updateToken = Gen24Service.GetUpdateToken(newValues, oldValues);

                        if (updateToken.HasValues)
                        {
                            var success = await UpdateInverter(uri, updateToken).ConfigureAwait(false);

                            if (success)
                            {
                                hasUpdates = true;
                            }
                        }
                    }
                }
            }
            finally

            {
                IsInUpdate = false;
            }
        }


        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private async ValueTask<Gen24InverterSettings> ReadDataFromInverter()
        {
            var mpptToken = (await WebClientService.GetFroniusJsonResponse("config/setup/powerunit/mppt").ConfigureAwait(false)).Token;
            var commonToken = (await WebClientService.GetFroniusJsonResponse("config/common").ConfigureAwait(false)).Token;
            var powerLimitToken = (await WebClientService.GetFroniusJsonResponse("config/powerlimits").ConfigureAwait(false)).Token;
            return Gen24InverterSettings.Parse(commonToken, mpptToken, powerLimitToken);
        }

        private void Undo()
        {
            Settings = (Gen24InverterSettings)oldSettings.Clone();

            if (Settings.PowerLimitSettings.Visualization.WattPeakReferenceValue == 0)
            {
                var system = IoC.Get<IDataCollectionService>().HomeAutomationSystem;

                Settings.PowerLimitSettings.Visualization.WattPeakReferenceValue =
                    (system?.Gen24Config?.InverterSettings?.Mppt?.Mppt1?.WattPeak ?? 0) +
                    (system?.Gen24Config?.InverterSettings?.Mppt?.Mppt2?.WattPeak ?? 0) +
                    (system?.Gen24Config2?.InverterSettings?.Mppt?.Mppt1?.WattPeak ?? 0) +
                    (system?.Gen24Config2?.InverterSettings?.Mppt?.Mppt2?.WattPeak ?? 0);
            }

            Title = $"{Loc.InverterSettings} - {oldSettings.SystemName}";

            SelectedPowerModeMppt1 = PowerModes.FirstOrDefault(pm => pm.Value == Settings.Mppt?.Mppt1?.PowerMode);
            SelectedDynamicPeakManagerModeMppt1 = DynamicPeakManagerModes.FirstOrDefault(mode => mode.Value == Settings.Mppt?.Mppt1?.DynamicPeakManager);
            WattPeakMppt1 = Settings.Mppt?.Mppt1?.WattPeak ?? 0;
            UpdateLogWattPeakMppt1();

            SelectedPowerModeMppt2 = PowerModes.FirstOrDefault(pm => pm.Value == Settings.Mppt?.Mppt2?.PowerMode);
            SelectedDynamicPeakManagerModeMppt2 = DynamicPeakManagerModes.FirstOrDefault(mode => mode.Value == Settings.Mppt?.Mppt2?.DynamicPeakManager);
            WattPeakMppt2 = Settings.Mppt?.Mppt2?.WattPeak ?? 0;
            UpdateLogWattPeakMppt2();

            SelectedPowerLimitMode = PowerLimitModes.First(plm => plm.Value == Settings.PowerLimitSettings.ExportLimits.ActivePower.PowerLimitMode);
            wattPeakReferenceValue = Settings.PowerLimitSettings.Visualization.WattPeakReferenceValue;
            softLimit = Settings.PowerLimitSettings.ExportLimits.ActivePower.SoftLimit.PowerLimit;
            hardLimit = Settings.PowerLimitSettings.ExportLimits.ActivePower.HardLimit.PowerLimit;
            NotifyOfPropertyChange(nameof(SoftLimit));
            NotifyOfPropertyChange(nameof(HardLimit));
            NotifyOfPropertyChange(nameof(WattPeakReferenceValue));
        }

        private void UpdateWattPeakMppt1()
        {
            WattPeakMppt1 = (uint)Math.Round(Math.Pow(10, LogWattPeakMppt1), MidpointRounding.AwayFromZero);

            if (Settings.Mppt?.Mppt1?.WattPeak is not null)
            {
                Settings.Mppt.Mppt1.WattPeak = WattPeakMppt1;
            }
        }

        private void UpdateLogWattPeakMppt1()
        {
            LogWattPeakMppt1 = WattPeakMppt1 == 0 ? -0.30980391997148633857556748281473 : Math.Log10(WattPeakMppt1);
            UpdateWattPeakMppt1();
        }

        private void UpdateWattPeakMppt2()
        {
            WattPeakMppt2 = (uint)Math.Round(Math.Pow(10, LogWattPeakMppt2), MidpointRounding.AwayFromZero);

            if (Settings.Mppt?.Mppt2?.WattPeak is not null)
            {
                Settings.Mppt.Mppt2.WattPeak = WattPeakMppt2;
            }
        }

        private void UpdateLogWattPeakMppt2()
        {
            LogWattPeakMppt2 = WattPeakMppt2 == 0 ? -0.30980391997148633857556748281473 : Math.Log10(WattPeakMppt2);
            UpdateWattPeakMppt2();
        }
    }
}
