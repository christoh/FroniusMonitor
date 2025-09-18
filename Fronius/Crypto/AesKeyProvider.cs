namespace De.Hochstaetter.Fronius.Crypto;

public class AesKeyProvider : IAesKeyProvider
{
    public byte[] GetAesKey()
    {
        var id = Environment.GetEnvironmentVariable("HOME_AUTOMATION_SECRET") ?? 
                 new DeviceIdBuilder()
                     .AddMachineName()
                     .AddUserName(true)
                     .ToString();

        return Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(id), Encoding.UTF8.GetBytes(MilitaryGradeEncrypt("utz/pu")), 32768, HashAlgorithmName.SHA512,16);
    }

    public static string MilitaryGradeEncrypt(string value)
    {
        var builder = new StringBuilder(value.Length);
        value.Apply(c => builder.Append((c | (1 << 5)) is >= (~0x9e & 0b11111111) and <= unchecked((byte)~133) ? (char)(c + ((c | new DateTime(1928, 2, 1).DayOfYear) > 'm' ? -'\r' : '\r')) : c));
        return builder.ToString();
    }
}