using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace De.Hochstaetter.HomeAutomationServer.Models.Authorization;

[Flags]
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
    [XmlAttribute] public string Username { get; set; } = string.Empty;
    [XmlAttribute] public string PasswordHash { get; set; } = string.Empty;
    [XmlAttribute] public string Salt { get; set; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(8));
    [XmlIgnore] [JsonIgnore] private byte[] SaltBytes => Convert.FromBase64String(Salt);

    [XmlAttribute] public Roles Roles { get; set; } = Roles.None;

    public void SetPassword(string password)
    {
        var hashBytes = GetHashBytes(password);
        PasswordHash = Convert.ToBase64String(SHA3_512.HashData(hashBytes));
    }

    public bool Authenticate(string password)
    {
        var hashBytes = GetHashBytes(password);
        var hash = Convert.ToBase64String(SHA3_512.HashData(hashBytes));
        return hash == PasswordHash;
    }

    private byte[] GetHashBytes(string password)
    {
        return SaltBytes.Concat(Encoding.UTF8.GetBytes(password)).ToArray();
    }
}
