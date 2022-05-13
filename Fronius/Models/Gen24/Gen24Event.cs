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
    private DateTime? activeUntil;
    [FroniusProprietaryImport("activeUntil", FroniusDataType.Root)]
    public DateTime? ActiveUntil
    {
        get => activeUntil;
        set => Set(ref activeUntil, value);
    }

    private DateTime? eventTime;
    [FroniusProprietaryImport("timestamp", FroniusDataType.Root)]
    public DateTime? EventTime
    {
        get => eventTime;
        set => Set(ref eventTime, value);
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

    private int? eventId;
    [FroniusProprietaryImport("eventID", FroniusDataType.Root)]
    public int? EventId
    {
        get => eventId;
        set => Set(ref eventId, value, () => NotifyOfPropertyChange(nameof(Code)));
    }

    public string Code => (string.IsNullOrEmpty(Prefix) ? string.Empty : $"{Prefix}-") + (EventId.HasValue ? EventId.Value.ToString(CultureInfo.InvariantCulture) : string.Empty);

    private Severity severity;
    [FroniusProprietaryImport("severity", FroniusDataType.Root)]
    public Severity Severity
    {
        get => severity;
        set => Set(ref severity, value);
    }

    public override string ToString() => $"{EventTime:g}: {prefix}-{EventId}: {label}";
}