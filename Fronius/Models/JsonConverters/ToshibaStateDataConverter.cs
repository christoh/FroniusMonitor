using System.Text.Json;

namespace De.Hochstaetter.Fronius.Models.JsonConverters
{
    internal class ToshibaStateDataConverter : JsonConverter<ToshibaHvacStateData>
    {
        public override ToshibaHvacStateData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var text = (reader.GetString() ?? throw new FormatException("No Json Text")).Trim();
            var result = new byte[text.Length / 2];

            for (var i = 0; i < text.Length; i += 2)
            {
                result[i >> 1] = byte.Parse(text[i..(i + 2)], NumberStyles.AllowHexSpecifier);
            }

            return new ToshibaHvacStateData {StateData = result};
        }

        public override void Write(Utf8JsonWriter writer, ToshibaHvacStateData value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
