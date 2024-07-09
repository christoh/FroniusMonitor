using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models.JsonConverters;

internal class ToshibaHexConverter<T> : JsonConverter<T> where T : IConvertible, IFormattable
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
            if (reader.TokenType == JsonTokenType.Number)
            {
                var longValue = reader.GetInt64();

                if (typeToConvert.IsEnum)
                {
                    return (T)Enum.Parse(typeToConvert,longValue.ToString(CultureInfo.InvariantCulture));
                }

                return (T)Convert.ChangeType(longValue, typeToConvert);
            }

            var text = reader.GetString() ?? throw new FormatException("Json text is null");
            var ulongValue = ulong.Parse(text, NumberStyles.AllowHexSpecifier | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture);

            return typeToConvert.IsEnum
                ? (T)Enum.Parse(typeToConvert, ulongValue.ToString(CultureInfo.InvariantCulture)) 
                : (T)Convert.ChangeType(ulongValue, typeToConvert);
        }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
            writer.WriteStringValue(value.ToString("x", CultureInfo.InvariantCulture));
        }
}