using System.Text.Json;

namespace De.Hochstaetter.Fronius.JsonConverters;

public class AwattarPriceInfoConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try
        {
            return reader.GetDecimal();
        }
        catch (Exception)
        {
            return 0.0m;
        }
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        try
        {
            writer.WriteNumberValue((decimal)value);
        }
        catch (Exception)
        {
            writer.WriteNumberValue(0.0);
        }
    }
}