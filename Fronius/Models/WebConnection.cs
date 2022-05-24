namespace De.Hochstaetter.Fronius.Models;

public class WebConnection : BindableBase, ICloneable
{
    private static readonly Aes aes;

    static WebConnection()
    {
        aes = Aes.Create();
        aes.KeySize = 128;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = SensorData.GetAesKey();
    }

    private string? baseUrl;

    [DefaultValue(null), XmlAttribute]
    public string? BaseUrl
    {
        get => baseUrl;
        set => Set(ref baseUrl, value);
    }

    private string? userName;

    [DefaultValue(null), XmlAttribute]
    public string? UserName
    {
        get => userName;
        set => Set(ref userName, value);
    }

    private string password = string.Empty;

    [XmlIgnore]
    public string Password
    {
        get => password;
        set => Set(ref password, value);
    }

    [DefaultValue(""), XmlAttribute("Password")]
    public string EncryptedPassword
    {
        get
        {
            try
            {
                using var encrypt = aes.CreateEncryptor();
                var bytes = Encoding.UTF8.GetBytes(Password);
                return Convert.ToBase64String(encrypt.TransformFinalBlock(bytes, 0, bytes.Length));
            }
            catch
            {
                return string.Empty;
            }
        }
        set
        {
            try
            {
                using var decrypt = aes.CreateDecryptor();
                var bytes = Convert.FromBase64String(value);
                Password = Encoding.UTF8.GetString(decrypt.TransformFinalBlock(bytes, 0, bytes.Length));
            }
            catch
            {
                Password = string.Empty;
            }
        }
    }

    public object Clone() => MemberwiseClone();
}
