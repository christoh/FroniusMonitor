using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace De.Hochstaetter.HomeAutomationServer.Models.Authorization;

public class User
{
    /// <summary>
    /// The account every server has and no user list contains: anyone may log in as it with the password
    /// <c>guest</c>, and it holds <see cref="Fronius.Models.WebApi.Roles.Guest"/> and nothing else. Use
    /// <see cref="AuthorizationExtensions.Find"/> to resolve a user name, never the user list alone, or this one
    /// is not found.
    /// </summary>
    public static User Guest { get; } = CreateGuest();

    /// <summary>
    /// Whether <paramref name="userName"/> names <see cref="Guest"/>, by the same comparison every other user name
    /// lookup in the server uses.
    /// </summary>
    public static bool IsGuest(string? userName) => string.Equals(userName, Guest.Username, StringComparison.OrdinalIgnoreCase);

    private string? passwordCache;

    [XmlAttribute]
    public string Username { get; set; } = string.Empty;

    [XmlAttribute, DefaultValue(null)]
    public string? ClearTextPassword
    {
        get => null;

        set
        {
            if (value != null)
            {
                PasswordHash = GetHash(value);
            }
        }
    }

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

    [XmlIgnore]
    [JsonIgnore]
    internal byte[] SaltBytes => Convert.FromBase64String(Salt);

    [XmlAttribute]
    public Roles Roles { get; set; } = Roles.None;

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

    /// <remarks>
    /// The salt is fixed rather than random, so that everything derived from it - the signature of a hub ticket and
    /// the key <c>IdentityController.RequestKey</c> hands the client - still means the same thing after the server
    /// has been restarted. There is nothing for a salt to protect here: the password is a constant in this file.
    /// <see cref="SetPassword"/> is not used for that reason, and the salt has to be in place before the password
    /// is set, because <see cref="ClearTextPassword"/> hashes with whatever salt the user has at that moment.
    /// </remarks>
    private static User CreateGuest()
    {
        var guest = new User { Username = "guest", Roles = Roles.Guest, Salt = Convert.ToBase64String("HomeAuto"u8) };
        guest.ClearTextPassword = "guest";
        return guest;
    }

    private string GetHash(string password)
    {
        var hashBytes = SaltBytes.Concat(Encoding.UTF8.GetBytes(password)).ToArray();
        return Convert.ToBase64String(SHA3_512.HashData(hashBytes));
    }
}
