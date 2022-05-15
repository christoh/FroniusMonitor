using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Extensions
{
    public static class EnumExtensions
    {
        public static string? ToDisplayName(this Enum @enum)
        {
            var type = @enum.GetType();
            return typeof(Resources).GetProperty($"{type.Name}_{Enum.GetName(type, @enum)}", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(null)?.ToString();
        }
    }
}
