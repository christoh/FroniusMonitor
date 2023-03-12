using System.Text.Json;

namespace De.Hochstaetter.Fronius.Models.JsonConverters
{
    internal class ToshibaDateTimeConverter:JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var text = reader.GetString();
            var success = DateTime.TryParse(text,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var date);
            return success ? date : throw new FormatException("Invalid Date");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToUniversalTime().ToString("M/d/yy hh:mm:ss tt", CultureInfo.InvariantCulture));
        }
    }
}
