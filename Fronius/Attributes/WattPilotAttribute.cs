namespace De.Hochstaetter.Fronius.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class WattPilotAttribute : Attribute
{
    public WattPilotAttribute(string tokenName)
    {
        TokenName=tokenName;
    }

    public string TokenName { get; init; }
}
