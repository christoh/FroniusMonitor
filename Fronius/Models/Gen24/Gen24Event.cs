namespace De.Hochstaetter.Fronius.Models.Gen24;

public enum Severity : byte
{
    None = 0,
    Error = 1,
    Warning = 2,
    Information = 3,
}

// app/assets/i18n/StateCodeTranslations/en.json

public partial class Gen24Event : BindableBase
{
    private readonly IGen24Service gen24Service = IoC.Get<IGen24Service>();

    [ObservableProperty]
    [FroniusProprietaryImport("activeUntil", FroniusDataType.Root)]
    public partial DateTime? ActiveUntil { get; set ; }

    [ObservableProperty]
    [FroniusProprietaryImport("timestamp", FroniusDataType.Root)]
    public partial DateTime? EventTime { get; set ; }

    [ObservableProperty]
    [FroniusProprietaryImport("label", FroniusDataType.Root)]
    public partial string? Label { get; set ; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Code),nameof(Message))]
    [FroniusProprietaryImport("prefix", FroniusDataType.Root)]
    public partial string? Prefix { get; set ; }

    [ObservableProperty]
    [FroniusProprietaryImport("severity", FroniusDataType.Root)]
    public partial Severity Severity { get; set ; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Code),nameof(Message))]
    [FroniusProprietaryImport("eventID", FroniusDataType.Root)]
    public partial uint? EventId { get; set ; }

    public string Code => (string.IsNullOrEmpty(Prefix) ? string.Empty : $"{Prefix}-") + (EventId.HasValue ? EventId.Value.ToString(CultureInfo.InvariantCulture) : string.Empty);

    public string Message => gen24Service.GetEventDescription(Code).GetAwaiter().GetResult();

    public override string ToString() => $"{EventTime:g}: {Prefix}-{EventId}: {Label}";
}