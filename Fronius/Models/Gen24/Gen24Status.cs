namespace De.Hochstaetter.Fronius.Models.Gen24;

public class Gen24Status : BindableBase
{
    private static readonly IGen24Service? gen24Service = IoC.TryGetRegistered<IGen24Service>();

    [FroniusProprietaryImport("id", FroniusDataType.Root)]
    public uint? Id
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("type", FroniusDataType.Root)]
    public DeviceType DeviceType
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(StatusMessage));
            NotifyOfPropertyChange(nameof(StatusMessageCaption));
        });
    }

    [FroniusProprietaryImport("statusMessage", FroniusDataType.Root)]
    public string? StatusCode
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(StatusMessage));
            NotifyOfPropertyChange(nameof(StatusMessageCaption));
        });
    }

    [FroniusProprietaryImport("status", FroniusDataType.Root)]
    public byte? Status
    {
        get;
        set => Set(ref field, value);
    }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public string? StatusMessage => gen24Service?.GetUiString((DeviceType == DeviceType.PowerMeter ? "POWERMETER" : "INVERTER") + ".DEVICESTATE." + StatusCode).GetAwaiter().GetResult();

    public string? StatusMessageCaption => Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName == "en" ? StatusMessage?[0].ToString().ToUpperInvariant() + StatusMessage?[1..] : StatusMessage;
}