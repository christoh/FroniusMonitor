namespace De.Hochstaetter.FroniusMonitor.ViewModels
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public class InverterSettingsViewModel : SettingsViewModelBase
    {
        private Gen24InverterSettings oldSettings = null!;

        public InverterSettingsViewModel(IWebClientService webClientService, IGen24JsonService gen24Service, IWattPilotService wattPilotService) : base(webClientService, gen24Service, wattPilotService) { }

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

        private ListItemModel<PowerLimitMode>? selectedPowerLimitMode;

        public ListItemModel<PowerLimitMode>? SelectedPowerLimitMode
        {
            get => selectedPowerLimitMode;
            set => Set(ref selectedPowerLimitMode, value, () =>
            {
                if (Settings?.ExportLimit?.Limit != null)
                {
                    Settings.ExportLimit.Limit.PowerLimitMode = value?.Value;
                }
            });
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
                    new ListItemModel<PowerLimitMode> { Value = PowerLimitMode.EntireSystem, DisplayName = await WebClientService.GetConfigString("EXPORTLIMIT", "WLIM_MAX_W").ConfigureAwait(false) },
                    new ListItemModel<PowerLimitMode> { Value = PowerLimitMode.WeakestPhase, DisplayName = await WebClientService.GetConfigString("EXPORTLIMIT", "WLIM_MAX_FEEDIN_PER_PHASE").ConfigureAwait(false) },
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

                if (!hasUpdates)
                {
                    ShowNoSettingsChanged();
                    return;
                }

                oldSettings = Settings;
                Undo();
                ToastText = Loc.SettingsSavedToInverter;
                return;

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
            var exportLimitToken = (await WebClientService.GetFroniusJsonResponse("config/powerlimits/exportLimits").ConfigureAwait(false)).Token;
            return Gen24InverterSettings.Parse(commonToken, mpptToken, exportLimitToken);
        }

        private void Undo()
        {
            Settings = (Gen24InverterSettings)oldSettings.Clone();
            Title = $"{Loc.InverterSettings} - {oldSettings.SystemName}";

            SelectedPowerModeMppt1 = PowerModes.FirstOrDefault(pm => pm.Value == Settings.Mppt?.Mppt1?.PowerMode);
            SelectedDynamicPeakManagerModeMppt1 = DynamicPeakManagerModes.FirstOrDefault(mode => mode.Value == Settings.Mppt?.Mppt1?.DynamicPeakManager);
            WattPeakMppt1 = Settings.Mppt?.Mppt1?.WattPeak ?? 0;
            UpdateLogWattPeakMppt1();

            SelectedPowerModeMppt2 = PowerModes.FirstOrDefault(pm => pm.Value == Settings.Mppt?.Mppt2?.PowerMode);
            SelectedDynamicPeakManagerModeMppt2 = DynamicPeakManagerModes.FirstOrDefault(mode => mode.Value == Settings.Mppt?.Mppt2?.DynamicPeakManager);
            WattPeakMppt2 = Settings.Mppt?.Mppt2?.WattPeak ?? 0;
            UpdateLogWattPeakMppt2();

            SelectedPowerLimitMode = PowerLimitModes.FirstOrDefault(plm => plm.Value == Settings?.ExportLimit?.Limit?.PowerLimitMode);
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
