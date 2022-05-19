namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

public enum ChargingRuleType
{
    [EnumParse(ParseAs = "CHARGE_MIN")] MinimumCharge,
    [EnumParse(ParseAs = "CHARGE_MAX")] MaximumCharge,
    [EnumParse(ParseAs = "DISCHARGE_MIN")] MinimumDischarge,
    [EnumParse(ParseAs = "DISCHARGE_MAX")] MaximumDischarge,
}

public class Gen24ChargingRule : BindableBase, ICloneable
{
    private DateTime? startTime;
    [FroniusProprietaryImport("TimeTable", "Start", Unit.Time)]
    public DateTime? StartTime
    {
        get => startTime;
        set => Set(ref startTime, value);
    }

    private DateTime? endTime;
    [FroniusProprietaryImport("TimeTable", "End", Unit.Time)]
    public DateTime? EndTime
    {
        get => endTime;
        set => Set(ref endTime, value);
    }

    private bool? monday;

    [FroniusProprietaryImport("Weekdays", "Mon")]
    public bool? Monday
    {
        get => monday;
        set => Set(ref monday, value);
    }

    private bool? tuesday;

    [FroniusProprietaryImport("Weekdays", "Tue")]
    public bool? Tuesday
    {
        get => tuesday;
        set => Set(ref tuesday, value);
    }

    private bool? wednesday;

    [FroniusProprietaryImport("Weekdays", "Wed")]
    public bool? Wednesday
    {
        get => wednesday;
        set => Set(ref wednesday, value);
    }

    private bool? thursday;

    [FroniusProprietaryImport("Weekdays", "Thu")]
    public bool? Thursday
    {
        get => thursday;
        set => Set(ref thursday, value);
    }

    private bool? friday;

    [FroniusProprietaryImport("Weekdays", "Fri")]
    public bool? Friday
    {
        get => friday;
        set => Set(ref friday, value);
    }

    private bool? saturday;

    [FroniusProprietaryImport("Weekdays", "Sat")]
    public bool? Saturday
    {
        get => saturday;
        set => Set(ref saturday, value);
    }

    private bool? sunday;

    [FroniusProprietaryImport("Weekdays", "Sun")]
    public bool? Sunday
    {
        get => sunday;
        set => Set(ref sunday, value);
    }

    private bool? isActive;

    [FroniusProprietaryImport("Active", FroniusDataType.Root)]
    public bool? IsActive
    {
        get => isActive;
        set => Set(ref isActive, value);
    }

    private int? power;

    [FroniusProprietaryImport("Power", FroniusDataType.Root)]
    public int? Power
    {
        get => power;
        set => Set(ref power, value);
    }

    private ChargingRuleType ruleType;
    [FroniusProprietaryImport("ScheduleType", FroniusDataType.Root)]
    public ChargingRuleType RuleType
    {
        get => ruleType;
        set => Set(ref ruleType, value);
    }

    public object Clone() => MemberwiseClone();

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static IList<Gen24ChargingRule> Parse(string json)
    {
        var token = JToken.Parse(json);

        if (token["timeofuse"] is not JArray array)
        {
            throw new NullReferenceException("No ChargingRule config present");
        }

        var gen24Service = IoC.Get<IGen24JsonService>();
        var result= new List<Gen24ChargingRule>(array.Count);
        result.AddRange(array.Select(timeOfUseToken => gen24Service.ReadFroniusData<Gen24ChargingRule>(timeOfUseToken)));

        return result;
    }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static JObject GetToken(IEnumerable<Gen24ChargingRule> rules)
    {
        var gen24Service = IoC.Get<IGen24JsonService>();
        var token = new JObject();
        var array = new JArray();
        token.Add("timeofuse", array);

        foreach (var rule in rules)
        {
            array.Add(gen24Service.GetUpdateToken(rule));
        }

        return token;
    }
}
