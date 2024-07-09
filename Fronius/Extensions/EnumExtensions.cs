namespace De.Hochstaetter.Fronius.Extensions;

public static class EnumExtensions
{
    public static string? ToDisplayName(this Enum @enum)
    {
            var type = @enum.GetType();
            var name = Enum.GetName(type, @enum);
            return typeof(Resources).GetProperty($"{type.Name}_{name}", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(null)?.ToString() ?? name;
        }

    public static string? ToToolTip(this Enum @enum)
    {
            var type = @enum.GetType();
            return typeof(Resources).GetProperty($"{type.Name}_{Enum.GetName(type, @enum)}_ToolTip", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(null)?.ToString();
        }
}