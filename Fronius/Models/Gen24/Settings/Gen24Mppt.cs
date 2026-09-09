namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum StringCombination : byte
{
    [EnumParse(ParseNumeric = true)] CombineStrings_12 = 1,
    [EnumParse(ParseNumeric = true)] CombineStrings_123 = 4,
    [EnumParse(ParseNumeric = true)] CombineStrings_1234 = 10,
    [EnumParse(ParseNumeric = true)] CombineStrings_12and34 = 11,
    [EnumParse(ParseNumeric = true)] CombineStrings_13 = 2,
    [EnumParse(ParseNumeric = true)] CombineStrings_134 = 8,
    [EnumParse(ParseNumeric = true)] CombineStrings_13and24 = 12,
    [EnumParse(ParseNumeric = true)] CombineStrings_14 = 5,
    [EnumParse(ParseNumeric = true)] CombineStrings_14and23 = 13,
    [EnumParse(ParseNumeric = true)] CombineStrings_23 = 3,
    [EnumParse(ParseNumeric = true)] CombineStrings_234 = 9,
    [EnumParse(ParseNumeric = true)] CombineStrings_24 = 6,
    [EnumParse(ParseNumeric = true)] CombineStrings_34 = 7,
    [EnumParse(ParseNumeric = true)] NotCombined = 0,
}

public partial class Gen24Mppt : BindableBase, ICloneable
{
    private static readonly IGen24JsonService gen24JsonService = IoC.TryGet<IGen24JsonService>()!;

    [ObservableProperty]
    [FroniusProprietaryImport("PV_MODE_COMBINE_U16", FroniusDataType.Root)]
    public partial StringCombination? StringCombination { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WattPeakTotal))]
    public partial Gen24Mppt1? Mppt1 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WattPeakTotal))]
    public partial Gen24Mppt2? Mppt2 { get; set; }

    public double? WattPeakTotal => Mppt1?.WattPeak + Mppt2?.WattPeak;

    public object Clone()
    {
        return new Gen24Mppt
        {
            StringCombination = StringCombination,
            Mppt1 = Mppt1?.Clone() as Gen24Mppt1,
            Mppt2 = Mppt2?.Clone() as Gen24Mppt2,
        };
    }

    public static Gen24Mppt Parse(JsonNode? token)
    {
        var result = gen24JsonService.ReadFroniusData<Gen24Mppt>(token);
        result.Mppt1 = gen24JsonService.ReadFroniusData<Gen24Mppt1>(token?["mppt1"]);
        result.Mppt2 = gen24JsonService.ReadFroniusData<Gen24Mppt2>(token?["mppt2"]);
        return result;
    }

    /// <summary>
    /// What has to go to <c>api/config/powerunit</c> to turn <paramref name="oldMppt"/> into this, and an empty
    /// object where the two say the same thing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two trackers are their own types with their own channel names - <c>PV_MODE_MPP_01_U16</c> against
    /// <c>PV_MODE_MPP_02_U16</c> - so each is asked for its own delta and the result is nested the way the
    /// inverter wants it: <c>{ "mppt": { "mppt1": …, "mppt2": … } }</c>.
    /// </para>
    /// <para>
    /// A tracker the other side does not have at all is left out rather than written whole. An inverter with one
    /// string reports one tracker, and inventing a second from our defaults is not an improvement on saying
    /// nothing. <see cref="StringCombination"/> is left out for the same reason: nothing edits it, so there is
    /// never anything to say about it.
    /// </para>
    /// </remarks>
    public JsonNode GetToken(Gen24Mppt? oldMppt = null)
    {
        var trackers = new JsonObject();
        Add("mppt1", Mppt1, oldMppt?.Mppt1);
        Add("mppt2", Mppt2, oldMppt?.Mppt2);
        return trackers.Count > 0 ? new JsonObject { { "mppt", trackers } } : new JsonObject();

        void Add(string name, Gen24MpptBase? wanted, Gen24MpptBase? current)
        {
            if (wanted is null || current is null || current.GetType() != wanted.GetType())
            {
                return;
            }

            var token = gen24JsonService.GetUpdateToken(wanted.GetType(), wanted, current);

            if (token.HasAnyValue())
            {
                trackers.Add(name, token);
            }
        }
    }
}
