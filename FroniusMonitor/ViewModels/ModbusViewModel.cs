namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class ModbusViewModel(
    IDataCollectionService dataCollectionService,
    IGen24Service gen24Service,
    IGen24JsonService gen24JsonService,
    IFritzBoxService fritzBoxService,
    IWattPilotService wattPilotService)
    : SettingsViewModelBase(dataCollectionService, gen24Service, gen24JsonService, fritzBoxService, wattPilotService)
{
    private Gen24ModbusSettings oldSettings = null!;

    public Gen24ModbusSettings Settings
    {
        get;
        set => Set(ref field, value);
    } = null!;

    public IReadOnlyList<ModbusInterfaceRole> ModBusInterfaceRoles => Enum.GetValues<ModbusInterfaceRole>();

    public IReadOnlyList<ModbusParity> ModBusParities => Enum.GetValues<ModbusParity>();

    public IReadOnlyList<SunspecMode> SunspecModes => Enum.GetValues<SunspecMode>();

    public string Title
    {
        get;
        set => Set(ref field, value);
    } = Loc.Modbus;

    public bool EnableTcp
    {
        get;
        set => Set(ref field, value);
    }

    public bool EnableDanger
    {
        get;
        set => Set(ref field, value, () =>
        {
            if (!value)
            {
                Settings.Rtu0 = oldSettings.Rtu0;
                Settings.Rtu1 = oldSettings.Rtu1;
                Settings.BaudRate = oldSettings.BaudRate;
                Settings.Parity = oldSettings.Parity;
            }
        });
    }

    [field: AllowNull, MaybeNull]
    public ICommand ApplyCommand => field ??= new NoParameterCommand(Apply);

    [field: AllowNull, MaybeNull]
    public ICommand UndoCommand => field ??= new NoParameterCommand(Undo);

    internal override async Task OnInitialize()
    {
        try
        {
            IsInUpdate = true;
            await base.OnInitialize().ConfigureAwait(false);
            using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            Gen24InverterSettings inverterSettings;

            try
            {
                var configToken = (await Gen24Service.GetFroniusJsonResponse("api/config/", token: tokenSource.Token).ConfigureAwait(false)).Token;
                oldSettings = Gen24ModbusSettings.Parse(configToken["modbus"]?["modbus"]);
                inverterSettings = Gen24InverterSettings.Parse(configToken);
            }
            catch (Exception ex)
            {
                IsInUpdate = false;
                
                ShowBox
                (
                    string.Format(Loc.InverterCommReadError, ex is TaskCanceledException ? Loc.InverterTimeout : ex.Message),
                    ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error
                );

                Close();
                return;
            }

            if (!string.IsNullOrWhiteSpace(inverterSettings.SystemName))
            {
                Title += $" - {inverterSettings.SystemName}";
            }

            Undo();
        }
        finally
        {
            IsInUpdate=false;
        }
    }

    private void Undo()
    {
        Settings = (Gen24ModbusSettings)oldSettings.Clone();
        EnableTcp = Settings.Mode is ModbusSlaveMode.Tcp or ModbusSlaveMode.Both;
    }

    public async void Apply()
    {
        IsInUpdate = true;

        try
        {
            var errors = FindVisualChildren<TextBox>().SelectMany(Validation.GetErrors).ToList();

            foreach (var error in errors)
            {
                if (error.BindingInError is BindingExpression { Target: FrameworkElement { IsVisible: false } } expression)
                {
                    var type = oldSettings.GetType();
                    var property = type.GetProperty(expression.ResolvedSourcePropertyName);

                    if (property != null)
                    {
                        var value = property.GetValue(oldSettings);
                        property.SetValue(Settings, value);
                    }
                }
            }

            var errorList = errors
                .Where(e => e.BindingInError is BindingExpression { Target: FrameworkElement { IsVisible: true } })
                .Select(e => e.ErrorContent.ToString()).ToList();

            if (errorList.Count > 0)
            {
                ShowBox
                (
                    $"{Loc.PleaseCorrectErrors}:{Environment.NewLine}{errorList.Aggregate(string.Empty, (c, n) => c + Environment.NewLine + "• " + n)}",
                    Loc.Error, MessageBoxButton.OK, MessageBoxImage.Error
                );

                return;
            }

            var isModbusRtu = Settings.Rtu0 == ModbusInterfaceRole.Slave || Settings.Rtu1 == ModbusInterfaceRole.Slave;
            var isAnyModbus = isModbusRtu || EnableTcp;

            Settings.InverterAddress = !isAnyModbus ? (byte)0 : (byte)1;

            if (EnableTcp && isModbusRtu)
            {
                Settings.Mode = ModbusSlaveMode.Both;
            }
            else if (EnableTcp)
            {
                Settings.Mode = ModbusSlaveMode.Tcp;
            }
            else if (isModbusRtu)
            {
                Settings.Mode = ModbusSlaveMode.Rtu;
            }
            else
            {
                Settings.Mode = ModbusSlaveMode.Off;
            }

            var updateToken = Settings.GetToken(oldSettings);

            if (!updateToken.Children().Any())
            {
                ShowNoSettingsChanged();
                return;
            }

            if (!await UpdateInverter("api/config/modbus", updateToken))
            {
                return;
            }

            oldSettings = Settings;
            Undo();
            ToastText = Loc.SettingsSavedToInverter;
        }
        finally
        {
            IsInUpdate = false;
        }
    }
}
