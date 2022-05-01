namespace De.Hochstaetter.Fronius.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
internal class EnumParseAttribute : Attribute
{
    public string? ParseAs { get; set; }
    public bool ParseNumeric { get; set; }
    public bool IsDefault { get; set; }
}
