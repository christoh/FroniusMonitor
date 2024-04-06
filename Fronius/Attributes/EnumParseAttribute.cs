﻿namespace De.Hochstaetter.Fronius.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
public class EnumParseAttribute : Attribute
{
    public string? ParseAs { get; set; }
    public bool ParseNumeric { get; set; }
    public bool IsDefault { get; set; }
}
