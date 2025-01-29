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
    public static Regex TimeRegex { get; } = new(@"^([0-9]{1,2}):([0-9]{1,2})$", RegexOptions.Compiled);

    [FroniusProprietaryImport("TimeTable", "Start", Unit.Time)]
    public string? StartTime
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(StartTimeDate)));
    }

    [FroniusProprietaryImport("TimeTable", "End", Unit.Time)]
    public string? EndTime
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(EndTimeDate)));
    }

    public DateTime? StartTimeDate => GetDate(StartTime);

    public DateTime? EndTimeDate => GetDate(EndTime);


    [FroniusProprietaryImport("Weekdays", "Mon")]
    public bool? Monday
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("Weekdays", "Tue")]
    public bool? Tuesday
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("Weekdays", "Wed")]
    public bool? Wednesday
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("Weekdays", "Thu")]
    public bool? Thursday
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("Weekdays", "Fri")]
    public bool? Friday
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("Weekdays", "Sat")]
    public bool? Saturday
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("Weekdays", "Sun")]
    public bool? Sunday
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("Active", FroniusDataType.Root)]
    public bool? IsActive
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("Power", FroniusDataType.Root)]
    public int? Power
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("ScheduleType", FroniusDataType.Root)]
    public ChargingRuleType? RuleType
    {
        get;
        set => Set(ref field, value);
    }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static BindableCollection<Gen24ChargingRule> Parse(JToken? token, SynchronizationContext? ctx)
    {
        var result = new BindableCollection<Gen24ChargingRule>(ctx);

        if (token?["timeofuse"] is not JArray array)
        {
            return result;
        }

        var gen24Service = IoC.Get<IGen24JsonService>();
        result.AddRange(array.Select(gen24Service.ReadFroniusData<Gen24ChargingRule>));
        return result;
    }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static JObject GetToken(IEnumerable<Gen24ChargingRule> rules)
    {
        var gen24Service = IoC.Get<IGen24JsonService>();
        var array = new JArray();
        rules.Apply(rule => array.Add(gen24Service.GetUpdateToken(rule)));
        return new JObject { { "timeofuse", array } };
    }

    public bool ConflictsWith(Gen24ChargingRule other)
    {
        return
            // Both rules are active
            IsActive.HasValue && IsActive.Value && other.IsActive.HasValue && other.IsActive.Value &&

            // Rules Overlap in time
            StartTimeDate < other.EndTimeDate && EndTimeDate > other.StartTimeDate &&

            RuleType.HasValue && other.RuleType.HasValue &&
            (
                // Same rule type
                RuleType.Value == other.RuleType.Value ||

                // Maximum is below Minimum
                RuleType.Value==ChargingRuleType.MaximumCharge && other.RuleType==ChargingRuleType.MinimumCharge && Power<other.Power ||
                // Minimum is above Maximum
                RuleType.Value==ChargingRuleType.MinimumCharge && other.RuleType==ChargingRuleType.MaximumCharge && Power>other.Power ||
                // Maximum is below Minimum
                RuleType.Value==ChargingRuleType.MaximumDischarge && other.RuleType==ChargingRuleType.MinimumDischarge && Power<other.Power ||
                // Minimum is above Maximum
                RuleType.Value==ChargingRuleType.MinimumDischarge && other.RuleType==ChargingRuleType.MaximumDischarge && Power>other.Power
            ) &&

            // Weekdays overlap
            (
                Monday.HasValue && Monday.Value && other.Monday.HasValue && other.Monday.Value ||
                Tuesday.HasValue && Tuesday.Value && other.Tuesday.HasValue && other.Tuesday.Value ||
                Wednesday.HasValue && Wednesday.Value && other.Wednesday.HasValue && other.Wednesday.Value ||
                Thursday.HasValue && Thursday.Value && other.Thursday.HasValue && other.Thursday.Value ||
                Friday.HasValue && Friday.Value && other.Friday.HasValue && other.Friday.Value ||
                Saturday.HasValue && Saturday.Value && other.Saturday.HasValue && other.Saturday.Value ||
                Sunday.HasValue && Sunday.Value && other.Sunday.HasValue && other.Sunday.Value
            );
    }

    public static DateTime? GetDate(string? timeString)
    {
        if (timeString == null)
        {
            return null;
        }

        var match = TimeRegex.Match(timeString);

        if (!match.Success)
        {
            return null;
        }

        var hours = byte.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var minutes = byte.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

        if (hours > 24 || minutes > 59 || hours == 24 && minutes != 0)
        {
            return null;
        }

        return DateTime.UtcNow.Date.AddHours(hours).AddMinutes(minutes);
    }

    public override string ToString() => $"{StartTimeDate:HH:mm}-{EndTimeDate:HH:mm}: {RuleType?.ToDisplayName()??"---"}: {Power??0} W";

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is Gen24ChargingRule other)
        {
            var type = GetType();

            if (type != other.GetType())
            {
                return false;
            }

            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);

            return fields.All(f =>
            {
                var thisValue = f.GetValue(this);
                var otherValue = f.GetValue(other);
                return ReferenceEquals(thisValue, other) || (thisValue?.Equals(otherValue) ?? false);
            });
        }

        return false;
    }

    public override int GetHashCode()
    {
        return StartTime?.GetHashCode() ?? 0 ^ EndTime?.GetHashCode() ?? 0 ^ Power?.GetHashCode() ?? 0 ^ RuleType?.GetHashCode() ?? 0;
    }

    public static bool operator ==(Gen24ChargingRule? left, Gen24ChargingRule? right) => ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
    public static bool operator !=(Gen24ChargingRule? left, Gen24ChargingRule? right) => !(left == right);

    public object Clone() => MemberwiseClone();
}
