namespace De.Hochstaetter.Fronius.Models.Settings;

public partial class WebConnection : BindableBase, ICloneable, IHaveDisplayName
{
    protected bool IsSlowPlatform;

    public static Aes Aes { get; private set; } = null!;

    static WebConnection()
    {
        CreateAes();
    }

    public static void InvalidateKey()
    {
        Aes.Dispose();
        CreateAes();
    }

    private static void CreateAes()
    {
        Aes = Aes.Create();
        Aes.KeySize = 128;
        Aes.Mode = CipherMode.ECB;
        Aes.Padding = PaddingMode.PKCS7;
        Aes.Key = IoC.Injector == null ? new byte[16] : IoC.Get<IAesKeyProvider>().GetAesKey();
    }

    [JsonIgnore]
    public string DisplayName => BaseUrl;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(DisplayName)), DefaultValue(""), XmlAttribute]
    public partial string BaseUrl { get; set; } = string.Empty;

    [ObservableProperty, DefaultValue(""), XmlAttribute]
    public partial string UserName { get; set; }  = string.Empty;

    [XmlIgnore, JsonIgnore]
    public string Password
    {
        get;
        set => Set(ref field, value, () => calculatedChecksum = null);
    } = string.Empty;

    [XmlAttribute, DefaultValue(null)]
    public string? ClearTextPassword
    {
        get => null;
        set
        {
            if (value != null)
            {
                Password = value;
                PasswordChecksum = CalculatedChecksum;
            }
        }
    }

    [ObservableProperty, XmlAttribute, DefaultValue(null)]
    public partial string? PasswordChecksum { get; set; } 

    [DefaultValue(""), XmlAttribute("Password")]
    public string EncryptedPassword
    {
        get
        {
            var result = string.Empty;

            try
            {
                using var encrypt = Aes.CreateEncryptor();
                var bytes = Encoding.UTF8.GetBytes(Password);
                result = Convert.ToBase64String(encrypt.TransformFinalBlock(bytes, 0, bytes.Length));
                return result;
            }
            catch (PlatformNotSupportedException)
            {
                return result;
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
            catch (PlatformNotSupportedException) { }
            catch (Exception)
            {
                Password = string.Empty;
            }
        }
    }

    private string? calculatedChecksum;

    [XmlIgnore, JsonIgnore]
    public string CalculatedChecksum
    {
        get
        {
            return calculatedChecksum ??= CalculateChecksum();

            string CalculateChecksum()
            {
                using var deriveBytes = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(Password), Aes.Key, IsSlowPlatform ? 256 : 131072, HashAlgorithmName.SHA256);
                return Convert.ToBase64String(deriveBytes.GetBytes(8));
            }
        }
    }

    public Task UpdateChecksumAsync() => Task.Run(() => PasswordChecksum = CalculatedChecksum);

    public override string ToString() => DisplayName;

    public object Clone() => MemberwiseClone();
}
