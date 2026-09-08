using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Gen24.Settings;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

/// <summary>
/// One time of use rule of an inverter, as the user edits it: a row of the schedule on the energy flow tab.
/// </summary>
/// <remarks>
/// <para>
/// The times and the power are <see cref="string"/> properties with the rules on them, not the
/// <see cref="int"/> and the times of <see cref="Gen24ChargingRule"/> - the text input rule of
/// <c>.claude/rules/ViewModelsForInteractionLogic.md</c>. Bound straight to the model a letter or an emptied box
/// would be refused by the binding itself, so nothing would notify, nothing would validate, and Undo would have
/// nothing to push back into the box.
/// </para>
/// <para>
/// The check boxes and the rule type bind to their values directly. A check box and a combo box can only ever
/// produce something the property accepts.
/// </para>
/// </remarks>
public sealed partial class Gen24ChargingRuleViewModel : ObservableValidator
{
    /// <summary>What a new rule starts as: the whole week, inactive, so it does nothing until it is filled in.</summary>
    public static Gen24ChargingRule NewRule() => new()
    {
        IsActive = false,
        RuleType = ChargingRuleType.MaximumCharge,
        Power = 50000,
        StartTime = "00:00",
        EndTime = "00:00",
        Monday = true,
        Tuesday = true,
        Wednesday = true,
        Thursday = true,
        Friday = true,
        Saturday = true,
        Sunday = true,
    };

    private readonly Gen24SelfConsumptionViewModel owner;

    public Gen24ChargingRuleViewModel(Gen24SelfConsumptionViewModel owner, Gen24ChargingRule rule)
    {
        this.owner = owner;
        Rule = (Gen24ChargingRule)rule.Clone();
        CopyFromRule();
    }

    /// <summary>The rule itself, for everything a control can only set to a valid value.</summary>
    [ObservableProperty]
    public partial Gen24ChargingRule Rule { get; set; }

    /// <summary>When the rule starts, as the box holds it. See the remarks on this class.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [TimeOfDay(AllowEmpty = false, PropertyDisplayNameResourceKey = nameof(Loc.Start))]
    public partial string? StartTimeText { get; set; }

    /// <inheritdoc cref="StartTimeText"/>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [TimeOfDay(AllowEmpty = false, PropertyDisplayNameResourceKey = nameof(Loc.End))]
    public partial string? EndTimeText { get; set; }

    /// <summary>
    /// How much the inverter may charge or discharge while the rule holds, in watts, as the box holds it.
    /// </summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MinMaxInt(0, 50000, AllowEmpty = false)]
    public partial string? PowerText { get; set; }

    public IReadOnlyList<EnumListItemModel<ChargingRuleType>> RuleTypes => owner.RuleTypes;

    /// <summary>
    /// What the rule says, for a message about it. The same wording <see cref="Gen24ChargingRule.ToString"/>
    /// gives, but from what the boxes hold rather than from the model - the point of a message is to name the row
    /// the user is looking at, and a refused box has not reached the model.
    /// </summary>
    public override string ToString() =>
        $"{StartTimeText}-{EndTimeText}: {Rule.RuleType?.ToDisplayName() ?? "---"}: {PowerText} W";

    [RelayCommand]
    private void Delete() => owner.Remove(this);

    /// <summary>The times and the power of the rule into the strings the boxes are bound to.</summary>
    public void CopyFromRule()
    {
        StartTimeText = Rule.StartTime;
        EndTimeText = Rule.EndTime;
        PowerText = NumericText.Of(Rule.Power);

        // Explicitly, because a setter validates only what it actually stores: writing the same value twice - at
        // load, or after Undo - changes nothing and would leave whatever error was there standing.
        ValidateAllProperties();
    }

    /// <summary>And back again, on the way to the inverter. Only ever called once the rules have passed.</summary>
    public void CopyToRule()
    {
        Rule.StartTime = StartTimeText;
        Rule.EndTime = EndTimeText;
        Rule.Power = (int?)NumericText.ToInteger(PowerText);
    }
}
