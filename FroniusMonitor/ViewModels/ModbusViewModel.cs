namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class ModbusViewModel : SettingsViewModelBase
{
    private Gen24ModbusSettings oldSettings = null!;
    private ModbusView view = null!;

    public ModbusViewModel(IDataCollectionService dataCollectionService, IWebClientService webClientService, IGen24JsonService gen24JsonService, IWattPilotService wattPilotService) : base(dataCollectionService, webClientService, gen24JsonService, wattPilotService) { }

    private Gen24ModbusSettings settings = null!;

    public Gen24ModbusSettings Settings
    {
        get => settings;
        set => Set(ref settings, value);
    }

    public IReadOnlyList<ModbusInterfaceRole> ModBusInterfaceRoles => Enum.GetValues<ModbusInterfaceRole>();

    public IReadOnlyList<ModbusParity> ModBusParities => Enum.GetValues<ModbusParity>();

    public IReadOnlyList<SunspecMode> SunspecModes => Enum.GetValues<SunspecMode>();

    private bool enableTcp;

    public bool EnableTcp
    {
        get => enableTcp;
        set => Set(ref enableTcp, value);
    }

    private bool enableDanger;

    public bool EnableDanger
    {
        get => enableDanger;
        set => Set(ref enableDanger, value, () =>
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

    private ICommand? applyCommand;
    public ICommand ApplyCommand => applyCommand ??= new NoParameterCommand(Apply);

    private ICommand? undoCommand;
    public ICommand UndoCommand => undoCommand ??= new NoParameterCommand(Undo);

    internal override async Task OnInitialize()
    {
        await base.OnInitialize().ConfigureAwait(false);
        var mainWindow = IoC.Get<MainWindow>();
        view = mainWindow.ModbusView;

        try
        {
            oldSettings = Gen24ModbusSettings.Parse((await WebClientService.GetFroniusStringResponse("config/modbus").ConfigureAwait(false)).JsonString);
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show
                (
                    view, string.Format(Resources.InverterCommReadError, ex.Message),
                    ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error
                );

                view.Close();
                mainWindow.Activate();
            });

            return;
        }

        Undo();
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
            var errors = view.FindVisualChildren<TextBox>().SelectMany(Validation.GetErrors).ToArray();

            foreach (var error in errors)
            {
                if (error.BindingInError is BindingExpression {Target: FrameworkElement {IsVisible: false}} expression)
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
                .Where(e => e.BindingInError is BindingExpression {Target: FrameworkElement {IsVisible: true}})
                .Select(e => e.ErrorContent.ToString()).ToArray();

            if (errorList.Length > 0)
            {
                MessageBox.Show
                (
                    view,
                    $"{Resources.PleaseCorrectErrors}:{Environment.NewLine}{errorList.Aggregate(string.Empty, (c, n) => c + Environment.NewLine + "• " + n)}",
                    Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error
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

            if (!await UpdateInverter("config/modbus", updateToken))
            {
                return;
            }

            oldSettings = Settings;
            Undo();
            ToastText = Resources.SettingsSavedToInverter;
        }
        finally
        {
            IsInUpdate = false;
        }
    }
}
