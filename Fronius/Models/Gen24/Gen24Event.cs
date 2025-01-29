namespace De.Hochstaetter.Fronius.Models.Gen24;

public enum Severity : byte
{
    None = 0,
    Error = 1,
    Warning = 2,
    Information = 3,
}

// app/assets/i18n/StateCodeTranslations/en.json

public class Gen24Event : BindableBase
{
    private readonly IGen24Service gen24Service = IoC.Get<IGen24Service>();

    [FroniusProprietaryImport("activeUntil", FroniusDataType.Root)]
    public DateTime? ActiveUntil
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("timestamp", FroniusDataType.Root)]
    public DateTime? EventTime
    {
        get;
        set => Set(ref field, value);
    }

    private string? label;
    [FroniusProprietaryImport("label", FroniusDataType.Root)]
    public string? Label
    {
        get => label;
        set => Set(ref label, value);
    }

    private string? prefix;
    [FroniusProprietaryImport("prefix", FroniusDataType.Root)]
    public string? Prefix
    {
        get => prefix;
        set => Set(ref prefix, value, () => NotifyOfPropertyChange(nameof(Code)));
    }

    [FroniusProprietaryImport("eventID", FroniusDataType.Root)]
    public uint? EventId
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(Code)));
    }

    public string Code => (string.IsNullOrEmpty(Prefix) ? string.Empty : $"{Prefix}-") + (EventId.HasValue ? EventId.Value.ToString(CultureInfo.InvariantCulture) : string.Empty);

    public string Message => gen24Service.GetEventDescription(Code).Result;

    [FroniusProprietaryImport("severity", FroniusDataType.Root)]
    public Severity Severity
    {
        get;
        set => Set(ref field, value);
    }

    public override string ToString() => $"{EventTime:g}: {prefix}-{EventId}: {label}";
}