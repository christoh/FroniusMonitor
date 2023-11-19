namespace De.Hochstaetter.Fronius.Models.Settings;

public class WebConnection : BindableBase, ICloneable, IHaveDisplayName
{
    public static Aes Aes;

    static WebConnection()
    {
        Aes = Aes.Create();
        Aes.KeySize = 128;
        Aes.Mode = CipherMode.ECB;
        Aes.Padding = PaddingMode.PKCS7;
        Aes.Key = IoC.Injector == null ? new byte[16] : IoC.Get<IAesKeyProvider>().GetAesKey();
    }

    public string DisplayName => BaseUrl;

    private string baseUrl = "";

    [DefaultValue(""), XmlAttribute]
    public string BaseUrl
    {
        get => baseUrl;
        set => Set(ref baseUrl, value);
    }

    private string userName = "";

    [DefaultValue(""), XmlAttribute]
    public string UserName
    {
        get => userName;
        set => Set(ref userName, value);
    }

    private string password = string.Empty;

    [XmlIgnore]
    public string Password
    {
        get => password;
        set => Set(ref password, value, () => calculatedChecksum = null);
    }

    private string? passwordChecksum;

    [XmlAttribute, DefaultValue(null)]
    public string? PasswordChecksum
    {
        get => passwordChecksum;
        set => Set(ref passwordChecksum, value);
    }

    [DefaultValue(""), XmlAttribute("Password")]
    public string EncryptedPassword
    {
        get
        {
            try
            {
                using var encrypt = Aes.CreateEncryptor();
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
                using var decrypt = Aes.CreateDecryptor();
                var bytes = Convert.FromBase64String(value);
                Password = Encoding.UTF8.GetString(decrypt.TransformFinalBlock(bytes, 0, bytes.Length));
            }
            catch
            {
                Password = string.Empty;
            }
        }
    }

    private string? calculatedChecksum;

    [XmlIgnore]
    public string CalculatedChecksum
    {
        get
        {
            return calculatedChecksum ??= CalculateChecksum();

            string CalculateChecksum()
            {
                using var deriveBytes = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(Password), Aes.Key, 131072, HashAlgorithmName.SHA256);
                return Convert.ToBase64String(deriveBytes.GetBytes(8));
            }
        }
    }

    public override string ToString() => DisplayName;

    public object Clone() => MemberwiseClone();
}
