namespace De.Hochstaetter.Fronius.Models.Settings;

public partial class WebConnection : BindableBase, ICloneable, IHaveDisplayName
{
    protected bool IsSlowPlatform;

    public static Aes Aes { get; private set; } = null!;

    static WebConnection() => CreateAes();

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

    private const int AesBlockLength = 16;
    private const int KeystreamBlockLength = 32;

    private static bool? hasAes;

    /// <summary>
    /// Whether this platform can carry out AES at all. A browser cannot: the Web Crypto API is asynchronous, so
    /// .NET exposes no symmetric cipher there, while the hash based primitives it compiles into the runtime are
    /// present - which is why the checksum works on a head where the encrypted password stayed empty.
    /// </summary>
    /// <remarks>
    /// Answered by trying it rather than by asking which platform this is: what matters is what works. The
    /// result is remembered, because on the platform that says no the answer costs an exception.
    /// </remarks>
    private static bool HasAes => hasAes ??= ProbeAes();

    private static bool ProbeAes()
    {
        try
        {
            using var encryptor = Aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(new byte[AesBlockLength], 0, AesBlockLength).Length > 0;
        }
        catch (PlatformNotSupportedException)
        {
            return false;
        }
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplayName => BaseUrl;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(DisplayName)), DefaultValue(""), XmlAttribute]
    public partial string BaseUrl { get; set; } = string.Empty;

    [ObservableProperty, DefaultValue(""), XmlAttribute]
    public partial string UserName { get; set; } = string.Empty;

    [XmlIgnore, System.Text.Json.Serialization.JsonIgnore]
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
            try
            {
                return Encrypt(Encoding.UTF8.GetBytes(Password)).ToBase64();
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
                Password = Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(value)));
            }
            catch
            {
                Password = string.Empty;
            }
        }
    }

    private static byte[] Encrypt(byte[] clearText)
    {
        if (!HasAes)
        {
            return XorWithKeystream(Pad(clearText));
        }

        using var encryptor = Aes.CreateEncryptor();
        var result = encryptor.TransformFinalBlock(clearText, 0, clearText.Length);
        return result;
    }

    private static byte[] Decrypt(byte[] cipherText)
    {
        if (!HasAes)
        {
            return Unpad(XorWithKeystream(cipherText));
        }

        using var decryptor = Aes.CreateDecryptor();
        var result = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);

        return result;
    }

    /// <summary>
    /// Stands in for AES where the platform has none. The keystream is HMAC-SHA256 over a block counter, keyed
    /// with the key AES would have used, so every key gives a different stream and no block of it repeats.
    /// </summary>
    /// <remarks>
    /// Encrypts and decrypts alike - XOR is its own inverse. This is reached on a browser only: everywhere else
    /// AES stays in charge, so the passwords in existing settings files keep reading as they always did.
    /// </remarks>
    private static byte[] XorWithKeystream(byte[] data)
    {
        var result = new byte[data.Length];

        for (var offset = 0; offset < data.Length; offset += KeystreamBlockLength)
        {
            var keystream = HMACSHA256.HashData(Aes.Key, BitConverter.GetBytes(offset / KeystreamBlockLength));

            for (var i = 0; i < keystream.Length && offset + i < data.Length; i++)
            {
                result[offset + i] = (byte)(data[offset + i] ^ keystream[i]);
            }
        }

        return result;
    }

    private static byte[] Pad(byte[] clearText)
    {
        // Never zero: a full block of padding is added rather than none, so there is always something to strip.
        var padding = AesBlockLength - clearText.Length % AesBlockLength;
        var result = new byte[clearText.Length + padding];
        clearText.CopyTo(result, 0);
        Array.Fill(result, (byte)padding, clearText.Length, padding);
        return result;
    }

    /// <remarks>
    /// The throw is what makes a wrong key forget the password instead of putting noise into the box: padding
    /// that another key produced hardly ever reads as valid, which is the same way AES fails here.
    /// </remarks>
    private static byte[] Unpad(byte[] padded)
    {
        var padding = padded.Length > 0 ? padded[^1] : 0;

        if (padding is < 1 or > AesBlockLength || padding > padded.Length)
        {
            throw new CryptographicException("Invalid padding");
        }

        for (var i = padded.Length - padding; i < padded.Length; i++)
        {
            if (padded[i] != padding)
            {
                throw new CryptographicException("Invalid padding");
            }
        }

        return padded[..^padding];
    }

    private string? calculatedChecksum;

    [XmlIgnore, System.Text.Json.Serialization.JsonIgnore]
    public string CalculatedChecksum
    {
        get
        {
            return calculatedChecksum ??= CalculateChecksum();

            string CalculateChecksum()
            {
                return Rfc2898DeriveBytes.Pbkdf2
                (
                    Encoding.UTF8.GetBytes(Password),
                    Aes.Key,
                    IsSlowPlatform ? 256 : 131072,
                    HashAlgorithmName.SHA256,
                    8
                ).ToBase64();
            }
        }
    }

    public Task UpdateChecksumAsync() => Task.Run(() => PasswordChecksum = CalculatedChecksum);

    public override string ToString() => DisplayName;

    public object Clone() => MemberwiseClone();
}
