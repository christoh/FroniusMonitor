using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace De.Hochstaetter.HomeAutomationServer.Models.Authorization;

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Roles
{
    None = 0,
    Guest = 1 << 0,
    User = 1 << 1,
    PowerUser = 1 << 2,
    Operator = 1 << 3,
    Administrator = 1 << 4,
    Developer = 1 << 5,
    All = Guest | User | PowerUser | Operator | Administrator | Developer,
}

public class User
{
    private string? passwordCache;

    [XmlAttribute] public string Username { get; set; } = string.Empty;

    [XmlAttribute]
    public string PasswordHash
    {
        get;
        set
        {
            field = value;
            passwordCache = null;
        }
    } = string.Empty;

    [XmlAttribute]
    public string Salt
    {
        get;
        set
        {
            field = value;
            passwordCache = null;
        }
    } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(8));

    [XmlIgnore] [JsonIgnore] internal byte[] SaltBytes => Convert.FromBase64String(Salt);

    [XmlAttribute] public Roles Roles { get; set; } = Roles.None;

    public void SetPassword(string password)
    {
        Salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(8));
        PasswordHash = GetHash(password);
        passwordCache = password;
    }

    public bool Authenticate(string password)
    {
        if (!string.IsNullOrEmpty(passwordCache) && password == passwordCache)
        {
            return true;
        }

        var hash = GetHash(password);

        if (hash != PasswordHash)
        {
            return false;
        }

        passwordCache = password;
        return true;
    }

    private string GetHash(string password)
    {
        var hashBytes = SaltBytes.Concat(Encoding.UTF8.GetBytes(password)).ToArray();
        return Convert.ToBase64String(SHA3_512.HashData(hashBytes));
    }
}
