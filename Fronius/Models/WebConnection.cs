namespace De.Hochstaetter.Fronius.Models;

public class WebConnection : BindableBase, ICloneable
{
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

    private string? password;
    [DefaultValue(null), XmlAttribute]
    public string? Password
    {
        get => password;
        set => Set(ref password, value);
    }

    public string EncryptedPassword
    {
        get
        {
            var aes = Aes.Create();
            aes.KeySize = 128;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Encoding.ASCII.GetBytes("Fronius GEN24 10"); // Replace by UUID
            var encryptor = aes.CreateEncryptor();
            var bytes = Encoding.UTF8.GetBytes(Password ?? new string((char)0x7280,1));
            return Convert.ToBase64String(encryptor.TransformFinalBlock(bytes, 0, bytes.Length));
        }
    }

    public object Clone() => MemberwiseClone();
}