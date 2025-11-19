namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

public enum ChargingRuleType
{
    [EnumParse(ParseAs = "CHARGE_MIN")] MinimumCharge,
    [EnumParse(ParseAs = "CHARGE_MAX")] MaximumCharge,
    [EnumParse(ParseAs = "DISCHARGE_MIN")] MinimumDischarge,
    [EnumParse(ParseAs = "DISCHARGE_MAX")] MaximumDischarge,
}

public partial class Gen24ChargingRule : BindableBase, ICloneable
{
    [GeneratedRegex(@"^([0-9]{1,2}):([0-9]{1,2})$", RegexOptions.Compiled)]
    public static partial Regex TimeRegex();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StartTimeDate))]
    [FroniusProprietaryImport("TimeTable", "Start", Unit.ParsableTime)]
    public partial string? StartTime { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EndTimeDate))]
    [FroniusProprietaryImport("TimeTable", "End", Unit.ParsableTime)]
    public partial string? EndTime { get; set; }

    public DateTime? StartTimeDate => GetDate(StartTime);

    public DateTime? EndTimeDate => GetDate(EndTime);


    [ObservableProperty]
    [FroniusProprietaryImport("Weekdays", "Mon")]
    public partial bool? Monday { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("Weekdays", "Tue")]
    public partial bool? Tuesday { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("Weekdays", "Wed")]
    public partial bool? Wednesday { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("Weekdays", "Thu")]
    public partial bool? Thursday { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("Weekdays", "Fri")]
    public partial bool? Friday { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("Weekdays", "Sat")]
    public partial bool? Saturday { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("Weekdays", "Sun")]
    public partial bool? Sunday { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("Active", FroniusDataType.Root)]
    public partial bool? IsActive { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("Power", FroniusDataType.Root)]
    public partial int? Power { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("ScheduleType", FroniusDataType.Root)]
    public partial ChargingRuleType? RuleType { get; set; }

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

        var match = TimeRegex().Match(timeString);

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

        if (obj is not Gen24ChargingRule other)
        {
            return false;
        }

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

    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        return StartTime?.GetHashCode() ?? 0 ^ EndTime?.GetHashCode() ?? 0 ^ Power?.GetHashCode() ?? 0 ^ RuleType?.GetHashCode() ?? 0;
    }

    public static bool operator ==(Gen24ChargingRule? left, Gen24ChargingRule? right) => ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
    public static bool operator !=(Gen24ChargingRule? left, Gen24ChargingRule? right) => !(left == right);

    public object Clone() => MemberwiseClone();
}
