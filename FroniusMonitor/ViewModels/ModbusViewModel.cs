using Microsoft.Extensions.Logging;

namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class ModbusViewModel(
    IDataCollectionService dataCollectionService,
    IGen24Service gen24Service,
    IGen24JsonService gen24JsonService,
    IFritzBoxService fritzBoxService,
    IWattPilotService wattPilotService,
    ILogger<ModbusViewModel> logger)
    : SettingsViewModelBase(dataCollectionService, gen24Service, gen24JsonService, fritzBoxService, wattPilotService, logger)
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

    /// <summary>
    /// The Modbus address of the smart meter, as the text box holds it.
    /// </summary>
    /// <remarks>
    /// A string, not the <c>byte?</c> of the settings, because a text input element never binds to a numeric
    /// property - see the rule about text input in <c>.claude/rules/ViewModelsForInteractionLogic.md</c>. Bound
    /// straight to a <c>byte?</c>, a letter or an emptied box is refused by the binding itself: the property never
    /// changes, nothing notifies, and Undo has nothing to push back into the box.
    /// The rule is on this property, and the settings carry the same one for the sake of the server; both take
    /// their limits from <see cref="Gen24ModbusSettings"/> so that they cannot drift apart.
    /// </remarks>
    [MinMaxInt(Gen24ModbusSettings.MinMeterAddress, Gen24ModbusSettings.MaxMeterAddress,
        MessageResourceKey = nameof(Loc.MeterAddressError))]
    public string? MeterAddressText
    {
        get;
        set => Set(ref field, value, postAction: () => ValidateProperty(value, nameof(MeterAddressText)));
    }

    /// <summary>
    /// The string control address, as the text box holds it. See <see cref="MeterAddressText"/>.
    /// </summary>
    /// <remarks>
    /// It may be left empty: only a Tauro has string controllers, so an inverter with none has nothing to put in
    /// the box. Neither address is required, in fact - an empty field becomes null, and a null is left out of the
    /// update token altogether, so it means "not mine to say" and never overwrites what the inverter holds.
    /// </remarks>
    [MinMaxInt(Gen24ModbusSettings.MinSunSpecAddress, Gen24ModbusSettings.MaxSunSpecAddress,
        MessageResourceKey = nameof(Loc.SunspecAddressError))]
    public string? SunSpecAddressText
    {
        get;
        set => Set(ref field, value, postAction: () => ValidateProperty(value, nameof(SunSpecAddressText)));
    }

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

    /// <summary>
    /// Unconditionally back to the settings this dialog started from. Every field, whatever it holds: the text
    /// properties are filled again as well, which is what reaches a box holding something the settings never took.
    /// </summary>
    private void Undo()
    {
        Settings = (Gen24ModbusSettings)oldSettings.Clone();
        MeterAddressText = NumericText.Of(Settings.MeterAddress);
        SunSpecAddressText = NumericText.Of(Settings.SunSpecAddress);
        EnableTcp = Settings.Mode is ModbusSlaveMode.Tcp or ModbusSlaveMode.Both;

        // Explicitly, because a setter validates only what it actually stores: writing the same value twice - null
        // over null, at load or after Undo - changes nothing and so would leave whatever error was there standing.
        ValidateAllProperties();
    }

    public async void Apply()
    {
        IsInUpdate = true;

        try
        {
            var errors = FindVisualChildren<TextBox>().SelectMany(Validation.GetErrors).ToList();

            foreach (var error in errors)
            {
                if (error.BindingInError is not BindingExpression { Target: FrameworkElement { IsVisible: false } } expression)
                {
                    continue;
                }

                var name = expression.ResolvedSourcePropertyName;
                var property = oldSettings.GetType().GetProperty(name);

                if (property != null)
                {
                    property.SetValue(Settings, property.GetValue(oldSettings));
                    continue;
                }

                // The two addresses are edited as text on this view model, so they are not found above. Same
                // intent: a field the user cannot see must not stop them saving.
                if (name == nameof(MeterAddressText))
                {
                    MeterAddressText = NumericText.Of(oldSettings.MeterAddress);
                }
                else if (name == nameof(SunSpecAddressText))
                {
                    SunSpecAddressText = NumericText.Of(oldSettings.SunSpecAddress);
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

            // The rules have passed, so the text of the two addresses is known to be a number in range - or empty,
            // which a string control address may be and which ends up as null.
            Settings.MeterAddress = (byte?)NumericText.ToInteger(MeterAddressText);
            Settings.SunSpecAddress = (byte?)NumericText.ToInteger(SunSpecAddressText);

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

            if (!updateToken.HasValues())
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
