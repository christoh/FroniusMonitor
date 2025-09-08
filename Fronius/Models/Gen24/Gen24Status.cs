namespace De.Hochstaetter.Fronius.Models.Gen24;

public partial class Gen24Status : BindableBase
{
    private static IGen24Service? gen24Service;

    public static IGen24Service? Gen24Service => gen24Service ??= IoC.TryGet<IGen24Service>();

    [ObservableProperty]
    [FroniusProprietaryImport("id", FroniusDataType.Root)]
    public partial uint? Id { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("type", FroniusDataType.Root)]
    public partial DeviceType DeviceType { get; set; }
    
    [ObservableProperty]
    [FroniusProprietaryImport("statusMessage", FroniusDataType.Root)]
    public partial string? StatusCode { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("status", FroniusDataType.Root)]
    public partial byte? Status { get; set; }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public string? StatusMessage() => Gen24Service?.GetUiString((DeviceType == DeviceType.PowerMeter ? "POWERMETER" : "INVERTER") + ".DEVICESTATE." + StatusCode).GetAwaiter().GetResult();

    public string? StatusMessageCaption() => Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName == "en" ? StatusMessage()?[0].ToString().ToUpperInvariant() + StatusMessage()?[1..] : StatusMessage();
}