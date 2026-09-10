namespace De.Hochstaetter.Fronius.Attributes;

/// <summary>
///     A list the charger also sends entry by entry, one key per property of one entry: the prefix, the index of
///     the entry, and the entry's own <see cref="WattPilotAttribute" /> name - <c>c0n</c> for the name of card 0.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class WattPilotIndexedAttribute(string prefix) : Attribute
{
    public string Prefix { get; init; } = prefix;
}
