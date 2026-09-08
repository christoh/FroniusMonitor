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
    [NotifyPropertyChangedFor(nameof(Code))]
    [FroniusProprietaryImport("prefix", FroniusDataType.Root)]
    public partial string? Prefix { get; set ; }

    [ObservableProperty]
    [FroniusProprietaryImport("severity", FroniusDataType.Root)]
    public partial Severity Severity { get; set ; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Code))]
    [FroniusProprietaryImport("eventID", FroniusDataType.Root)]
    public partial uint? EventId { get; set ; }

    /// <summary>What the inverter calls this event, as <c>prefix-eventID</c>. The key its description is under.</summary>
    public string Code => (string.IsNullOrEmpty(Prefix) ? string.Empty : $"{Prefix}-") + (EventId.HasValue ? EventId.Value.ToString(CultureInfo.InvariantCulture) : string.Empty);

    /// <summary>
    /// What <see cref="Code"/> means, in words - filled in by whoever displays the event, and not sent anywhere.
    /// </summary>
    /// <remarks>
    /// The description comes from a translation file downloaded from the inverter, in the language of the app
    /// asking for it, so it cannot be read here: this model is also what the server sends a client, and a client
    /// wants the text in its own language rather than the one the server happens to run in. FroniusMonitor talks
    /// to the inverter itself and fills this from <see cref="IGen24Service.GetEventDescription"/>; the Avalonia
    /// client localizes <see cref="Code"/> through its own <c>IGen24LocalizationService</c> instead.
    ///
    /// It used to be a getter that reached for an <see cref="IGen24Service"/> through the static injector and
    /// blocked on it. That worked only in the app whose injector holds the one service bound to the inverter
    /// being looked at: the server registers that service per request and hands out one with no connection, whose
    /// <c>GetEventDescription</c> waits for a connection that never arrives - and it was read while the response
    /// was being serialized, so the request never came back. On a client, where nothing implements the interface
    /// at all, constructing an event threw.
    /// </remarks>
    [ObservableProperty]
    [JsonIgnore]
    public partial string? Message { get; set; }

    public override string ToString() => $"{EventTime:g}: {Prefix}-{EventId}: {Label}";
}