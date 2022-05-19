﻿namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class ModbusViewModel : ViewModelBase
{
    private readonly IWebClientService webClientService;
    private Gen24ModbusSettings oldSettings = null!;
    private ModbusView view = null!;

    public ModbusViewModel(IWebClientService webClientService)
    {
        this.webClientService = webClientService;
    }

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
        view = IoC.Get<MainWindow>().ModbusView;
        oldSettings = Gen24ModbusSettings.Parse(await webClientService.GetFroniusJsonResponse("config/modbus").ConfigureAwait(false));
        Undo();
    }

    private void Undo()
    {
        Settings = (Gen24ModbusSettings)oldSettings.Clone();
        EnableTcp = Settings.Mode is ModbusSlaveMode.Tcp or ModbusSlaveMode.Both;
    }

    public async void Apply()
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
            await Dispatcher.InvokeAsync(() => MessageBox.Show(view, Resources.NoSettingsChanged, Resources.Warning, MessageBoxButton.OK, MessageBoxImage.Warning));
            return;
        }

        try
        {
            var _ = await webClientService.GetFroniusJsonResponse("config/modbus", updateToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() => MessageBox.Show
            (
                view, string.Format(Resources.InverterCommError, ex.Message) + Environment.NewLine + Environment.NewLine + updateToken,
                ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error
            ));

            return;
        }

        oldSettings = Settings;
        Undo();
    }
}