namespace De.Hochstaetter.Fronius.Extensions;

public static class EnumExtensions
{
    extension(Enum @enum)
    {
        public string? ToDisplayName()
        {
            var type = @enum.GetType();
            var name = Enum.GetName(type, @enum);
            return typeof(Resources).GetProperty($"{type.Name}_{name}", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(null)?.ToString() ?? name;
        }

        public string? ToToolTip()
        {
            var type = @enum.GetType();
            return typeof(Resources).GetProperty($"{type.Name}_{Enum.GetName(type, @enum)}_ToolTip", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(null)?.ToString();
        }
    }
}